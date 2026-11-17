using Microsoft.EntityFrameworkCore;
using PRM_PRJ.Enums;
using PRM_PRJ.Models;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace PRM_PRJ.Seeder
{
    public class DbInitializer
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly AppDbContext _context;
        private readonly ILogger<DbInitializer> _logger;

        public DbInitializer(AppDbContext context, ILogger<DbInitializer> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            await _context.Database.MigrateAsync();

            var dataDirectory = ResolveDataDirectory();
            var subjectsSeed = LoadJson<List<SubjectSeedItem>>(Path.Combine(dataDirectory, "subjects.json"));
            var classesSeed = LoadJson<List<ClassSeedItem>>(Path.Combine(dataDirectory, "classes.json"));
            var settings = LoadJson<SeederSettings>(Path.Combine(dataDirectory, "seeder-settings.json"));

            ValidateConfig(subjectsSeed, classesSeed, settings);

            var shouldReseed = settings.ForceReseedOnStartup || await NeedsReseedAsync(subjectsSeed, classesSeed, settings);
            if (!shouldReseed)
            {
                var minValidDate = new DateTime(2000, 1, 1);
                var legacyRowsUpdated = await _context.Timetables
                    .Where(x => x.WeekStartDate < minValidDate)
                    .ExecuteUpdateAsync(s => s.SetProperty(t => t.WeekStartDate, settings.AttendanceWeekStart.Date));

                if (legacyRowsUpdated > 0)
                {
                    _logger.LogInformation("Normalized WeekStartDate for {Count} existing Timetable rows.", legacyRowsUpdated);
                }

                _logger.LogInformation("Seeder skipped because database already matches JSON configuration.");
                return;
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            await ClearSeedDataAsync();

            var random = new Random(settings.RandomSeed);
            var roles = await SeedRolesAsync();

            var adminUser = CreateUser("admin", "0900000000", BCrypt.Net.BCrypt.HashPassword("123456"));
            _context.Users.Add(adminUser);
            _context.UserRoles.Add(new UserRole { User = adminUser, Role = roles["Admin"] });

            var adminUserId = adminUser.Id;

            var teacherUsers = new List<User>();
            for (var i = 0; i < settings.TeacherNames.Count; i++)
            {
                var user = CreateUser(
                    username: $"teacher{i + 1:D3}",
                    phoneNumber: BuildPhone("091", i + 1),
                    passwordHash: BCrypt.Net.BCrypt.HashPassword("123456"));

                teacherUsers.Add(user);
                _context.Users.Add(user);
                _context.UserRoles.Add(new UserRole { User = user, Role = roles["Teacher"] });
            }

            await _context.SaveChangesAsync();

            var teachers = new List<Teacher>();
            for (var i = 0; i < settings.TeacherNames.Count; i++)
            {
                teachers.Add(new Teacher
                {
                    UserId = teacherUsers[i].Id,
                    FullName = settings.TeacherNames[i].Trim(),
                    Phone = BuildPhone("097", i + 1),
                    Email = BuildTeacherEmail(settings.TeacherNames[i], i + 1)
                });
            }

            _context.Teachers.AddRange(teachers);

            var subjects = subjectsSeed
                .Select(x => new Subject
                {
                    Name = x.Name.Trim()
                })
                .ToList();
            _context.Subjects.AddRange(subjects);

            var schoolYear = new SchoolYear { Name = settings.SchoolYearName.Trim() };
            _context.SchoolYears.Add(schoolYear);

            var semesters = settings.SemesterNames
                .Select(x => new Semester
                {
                    Name = x.Trim(),
                    SchoolYear = schoolYear
                })
                .ToList();
            _context.Semesters.AddRange(semesters);

            await _context.SaveChangesAsync();

            var teachersByName = teachers.ToDictionary(x => x.FullName, StringComparer.OrdinalIgnoreCase);
            var classes = classesSeed
                .Select((seed, index) =>
                {
                    Guid? homeroomTeacherId = null;
                    if (!string.IsNullOrWhiteSpace(seed.HomeroomTeacher) &&
                        teachersByName.TryGetValue(seed.HomeroomTeacher.Trim(), out var homeroomTeacher))
                    {
                        homeroomTeacherId = homeroomTeacher.Id;
                    }
                    else
                    {
                        homeroomTeacherId = teachers[index % teachers.Count].Id;
                    }

                    return new Class
                    {
                        Name = seed.Name.Trim(),
                        HomeroomTeacherId = homeroomTeacherId
                    };
                })
                .ToList();

            _context.Classes.AddRange(classes);
            await _context.SaveChangesAsync();

            var classByName = classes.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
            var students = new List<Student>();
            var studentParents = new List<StudentParent>();
            var studentCounter = 1;
            var parentCounter = 1;

            foreach (var classSeed in classesSeed)
            {
                var classroom = classByName[classSeed.Name.Trim()];
                for (var i = 1; i <= classSeed.StudentCount; i++)
                {
                    var studentUser = CreateUser(
                        username: $"student_{SanitizeToken(classroom.Name)}_{i:D2}",
                        phoneNumber: BuildPhone("094", studentCounter++),
                        passwordHash: BCrypt.Net.BCrypt.HashPassword("123456"));

                    _context.Users.Add(studentUser);
                    _context.UserRoles.Add(new UserRole { User = studentUser, Role = roles["Student"] });

                    var parentUser = CreateUser(
                        username: $"parent_{SanitizeToken(classroom.Name)}_{i:D2}",
                        phoneNumber: BuildPhone("093", parentCounter++),
                        passwordHash: BCrypt.Net.BCrypt.HashPassword("123456"));

                    _context.Users.Add(parentUser);
                    _context.UserRoles.Add(new UserRole { User = parentUser, Role = roles["Parent"] });

                    var isMale = random.Next(0, 2) == 0;

                    var student = new Student
                    {
                        FullName = GenerateStudentName(random, isMale),
                        DateOfBirth = RandomDate(random, new DateTime(2008, 1, 1), new DateTime(2010, 12, 31)),
                        Gender = isMale ? "Nam" : "Nu",
                        ClassId = classroom.Id,
                        UserId = studentUser.Id,
                        StudentCode = $"{classroom.Name}-{i:D3}",
                        IsActive = true
                    };

                    students.Add(student);

                    studentParents.Add(new StudentParent
                    {
                        Student = student,
                        ParentUserId = parentUser.Id
                    });
                }
            }

            _context.Students.AddRange(students);
            await _context.SaveChangesAsync();

            _context.StudentParents.AddRange(studentParents);
            await _context.SaveChangesAsync();

            var teacherSubjects = BuildTeacherSubjects(teachers, subjects, settings.TeachersPerSubject);
            _context.TeacherSubjects.AddRange(teacherSubjects);
            await _context.SaveChangesAsync();

            var timetables = BuildTimetables(classes, subjects, teacherSubjects, settings, random);
            _context.Timetables.AddRange(timetables);
            await _context.SaveChangesAsync();

            var attendances = BuildAttendances(students, timetables, settings, random);
            _context.Attendances.AddRange(attendances);
            await _context.SaveChangesAsync();

            var scores = BuildScores(students, semesters, timetables, settings, random);
            _context.Scores.AddRange(scores);
            await _context.SaveChangesAsync();

            var conversations = BuildConversations(students);
            _context.Conversations.AddRange(conversations);
            await _context.SaveChangesAsync();

            var messages = BuildMessages(
                conversations,
                students,
                studentParents,
                classes,
                teachers,
                adminUserId,
                settings,
                random);
            _context.Messages.AddRange(messages);

            var pickups = BuildPickups(students, settings, random);
            _context.Pickups.AddRange(pickups);

            var meals = BuildMeals(settings);
            _context.Meals.AddRange(meals);
            await _context.SaveChangesAsync();

            var studentMeals = BuildStudentMeals(students, meals, settings, random);
            _context.StudentMeals.AddRange(studentMeals);
            await _context.SaveChangesAsync();

            var allUsers = await _context.Users.AsNoTracking().ToListAsync();
            var refreshTokens = BuildRefreshTokens(allUsers, settings, random);
            _context.RefreshTokens.AddRange(refreshTokens);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            _logger.LogInformation(
                "JSON seeding completed. Subjects={SubjectCount}, Classes={ClassCount}, Students={StudentCount}, Timetables={TimetableCount}, Attendances={AttendanceCount}, Scores={ScoreCount}, Conversations={ConversationCount}, Messages={MessageCount}, Pickups={PickupCount}, Meals={MealCount}, StudentMeals={StudentMealCount}, RefreshTokens={RefreshTokenCount}.",
                subjects.Count,
                classes.Count,
                students.Count,
                timetables.Count,
                attendances.Count,
                scores.Count,
                conversations.Count,
                messages.Count,
                pickups.Count,
                meals.Count,
                studentMeals.Count,
                refreshTokens.Count);
        }

        private async Task<Dictionary<string, Role>> SeedRolesAsync()
        {
            var roles = new[]
            {
                new Role { Name = "Admin" },
                new Role { Name = "Teacher" },
                new Role { Name = "Student" },
                new Role { Name = "Parent" }
            };

            _context.Roles.AddRange(roles);
            await _context.SaveChangesAsync();

            return roles.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
        }

        private async Task<bool> NeedsReseedAsync(
            IReadOnlyCollection<SubjectSeedItem> subjectSeeds,
            IReadOnlyCollection<ClassSeedItem> classSeeds,
            SeederSettings settings)
        {
            var dbSubjectNames = await _context.Subjects.Select(x => x.Name).ToListAsync();
            if (!SetEquals(dbSubjectNames, subjectSeeds.Select(x => x.Name)))
            {
                return true;
            }

            var dbClassNames = await _context.Classes.Select(x => x.Name).ToListAsync();
            if (!SetEquals(dbClassNames, classSeeds.Select(x => x.Name)))
            {
                return true;
            }

            var dbSchoolYearName = await _context.SchoolYears.Select(x => x.Name).FirstOrDefaultAsync();
            if (!string.Equals(dbSchoolYearName, settings.SchoolYearName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var dbSemesterNames = await _context.Semesters.Select(x => x.Name).ToListAsync();
            if (!SetEquals(dbSemesterNames, settings.SemesterNames))
            {
                return true;
            }

            var hasCoreData =
                await _context.Teachers.AnyAsync() &&
                await _context.Students.AnyAsync() &&
                await _context.StudentParents.AnyAsync() &&
                await _context.TeacherSubjects.AnyAsync() &&
                await _context.Timetables.AnyAsync() &&
                await _context.Attendances.AnyAsync() &&
                await _context.Scores.AnyAsync() &&
                await _context.Conversations.AnyAsync() &&
                await _context.Messages.AnyAsync() &&
                await _context.Pickups.AnyAsync() &&
                await _context.Meals.AnyAsync() &&
                await _context.StudentMeals.AnyAsync() &&
                await _context.RefreshTokens.AnyAsync();

            return !hasCoreData;
        }

        private async Task ClearSeedDataAsync()
        {
            await _context.Messages.ExecuteDeleteAsync();
            await _context.Conversations.ExecuteDeleteAsync();
            await _context.Pickups.ExecuteDeleteAsync();
            await _context.StudentMeals.ExecuteDeleteAsync();
            await _context.Meals.ExecuteDeleteAsync();

            await _context.Attendances.ExecuteDeleteAsync();
            await _context.Scores.ExecuteDeleteAsync();
            await _context.Timetables.ExecuteDeleteAsync();
            await _context.TeacherSubjects.ExecuteDeleteAsync();

            await _context.RefreshTokens.ExecuteDeleteAsync();
            await _context.UserRoles.ExecuteDeleteAsync();

            await _context.UserNotificationReads.ExecuteDeleteAsync();
            await _context.AdminNotifications.ExecuteDeleteAsync();

            await _context.Applications.ExecuteDeleteAsync();

            await _context.StudentParents.ExecuteDeleteAsync();
            await _context.Students.ExecuteDeleteAsync();
            await _context.Classes.ExecuteDeleteAsync();
            await _context.Teachers.ExecuteDeleteAsync();
            await _context.Semesters.ExecuteDeleteAsync();
            await _context.SchoolYears.ExecuteDeleteAsync();

            await _context.Users.ExecuteDeleteAsync();
            await _context.Roles.ExecuteDeleteAsync();
            await _context.Subjects.ExecuteDeleteAsync();
        }
        private static List<TeacherSubject> BuildTeacherSubjects(
            IReadOnlyList<Teacher> teachers,
            IReadOnlyList<Subject> subjects,
            int teachersPerSubject)
        {
            var takeCount = Math.Clamp(teachersPerSubject, 1, teachers.Count);
            var result = new List<TeacherSubject>();
            var usedPairs = new HashSet<(Guid TeacherId, int SubjectId)>();

            for (var subjectIndex = 0; subjectIndex < subjects.Count; subjectIndex++)
            {
                var usedTeacherIndexes = new HashSet<int>();
                for (var offset = 0; offset < takeCount; offset++)
                {
                    var teacherIndex = (subjectIndex + (offset * 2)) % teachers.Count;
                    while (!usedTeacherIndexes.Add(teacherIndex))
                    {
                        teacherIndex = (teacherIndex + 1) % teachers.Count;
                    }

                    var pair = (teachers[teacherIndex].Id, subjects[subjectIndex].Id);
                    if (usedPairs.Add(pair))
                    {
                        result.Add(new TeacherSubject
                        {
                            TeacherId = pair.Item1,
                            SubjectId = pair.Item2
                        });
                    }
                }
            }

            var teacherIdsWithSubject = result.Select(x => x.TeacherId).ToHashSet();
            var subjectLoad = result
                .GroupBy(x => x.SubjectId)
                .ToDictionary(x => x.Key, x => x.Count());

            foreach (var teacher in teachers.Where(x => !teacherIdsWithSubject.Contains(x.Id)))
            {
                var leastLoadedSubjectId = subjectLoad
                    .OrderBy(x => x.Value)
                    .ThenBy(x => x.Key)
                    .First()
                    .Key;

                result.Add(new TeacherSubject
                {
                    TeacherId = teacher.Id,
                    SubjectId = leastLoadedSubjectId
                });
                subjectLoad[leastLoadedSubjectId]++;
            }

            return result;
        }

        private static List<Timetable> BuildTimetables(
            IReadOnlyList<Class> classes,
            IReadOnlyList<Subject> subjects,
            IReadOnlyList<TeacherSubject> teacherSubjects,
            SeederSettings settings,
            Random random)
        {
            var slots = BuildSlotTemplates(settings);
            var totalWeeklyPeriods = slots.Count;

            var subjectByName = subjects.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
            var weeklyTemplate = new Dictionary<int, int>();
            foreach (var frequency in settings.WeeklySubjectFrequency)
            {
                if (!subjectByName.TryGetValue(frequency.Key.Trim(), out var subject))
                {
                    throw new InvalidOperationException($"Subject '{frequency.Key}' in weeklySubjectFrequency does not exist in subjects.json.");
                }

                weeklyTemplate[subject.Id] = frequency.Value;
            }

            if (weeklyTemplate.Values.Sum() != totalWeeklyPeriods)
            {
                throw new InvalidOperationException(
                    $"Total weekly subject periods ({weeklyTemplate.Values.Sum()}) must equal timetable slots ({totalWeeklyPeriods}).");
            }

            var subjectTeachers = teacherSubjects
                .GroupBy(x => x.SubjectId)
                .ToDictionary(
                    x => x.Key,
                    x => x.Select(y => y.TeacherId).Distinct().ToList());

            const int maxAttempts = 400;
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                var remainingByClass = classes.ToDictionary(
                    x => x.Id,
                    _ => weeklyTemplate.ToDictionary(k => k.Key, k => k.Value));

                var busyTeachersBySlot = new Dictionary<(DayOfWeek Day, int PeriodIndex), HashSet<Guid>>();
                var timetableRows = new List<Timetable>();
                var failed = false;

                foreach (var slot in slots)
                {
                    var classOrder = classes
                        .OrderBy(x => CountAvailableOptions(
                            classId: x.Id,
                            slot.Day,
                            slot.PeriodIndex,
                            remainingByClass,
                            subjectTeachers,
                            busyTeachersBySlot))
                        .ToList();

                    foreach (var classroom in classOrder)
                    {
                        var found = TryPickAssignment(
                            classId: classroom.Id,
                            slot.Day,
                            slot.PeriodIndex,
                            remainingByClass,
                            subjectTeachers,
                            busyTeachersBySlot,
                            random,
                            out var subjectId,
                            out var teacherId);

                        if (!found)
                        {
                            failed = true;
                            break;
                        }

                        remainingByClass[classroom.Id][subjectId]--;

                        var slotKey = (slot.Day, slot.PeriodIndex);
                        if (!busyTeachersBySlot.TryGetValue(slotKey, out var busyTeacherSet))
                        {
                            busyTeacherSet = new HashSet<Guid>();
                            busyTeachersBySlot[slotKey] = busyTeacherSet;
                        }
                        busyTeacherSet.Add(teacherId);

                        timetableRows.Add(new Timetable
                        {
                            ClassId = classroom.Id,
                            SubjectId = subjectId,
                            TeacherId = teacherId,
                            DayOfWeek = slot.Day,
                            StartTime = slot.StartTime,
                            EndTime = slot.EndTime,
                            WeekStartDate = settings.AttendanceWeekStart.Date
                        });
                    }

                    if (failed)
                    {
                        break;
                    }
                }

                var allPeriodsUsed = remainingByClass.All(x => x.Value.Values.All(y => y == 0));
                if (!failed && allPeriodsUsed)
                {
                    return timetableRows;
                }
            }

            throw new InvalidOperationException("Unable to generate conflict-free timetable after many attempts.");
        }

        private static int CountAvailableOptions(
            int classId,
            DayOfWeek day,
            int periodIndex,
            IReadOnlyDictionary<int, Dictionary<int, int>> remainingByClass,
            IReadOnlyDictionary<int, List<Guid>> subjectTeachers,
            IReadOnlyDictionary<(DayOfWeek Day, int PeriodIndex), HashSet<Guid>> busyTeachersBySlot)
        {
            var slotKey = (day, periodIndex);
            var busySet = busyTeachersBySlot.TryGetValue(slotKey, out var teachersInSlot)
                ? teachersInSlot
                : new HashSet<Guid>();

            var options = 0;
            foreach (var entry in remainingByClass[classId])
            {
                if (entry.Value <= 0)
                {
                    continue;
                }

                if (!subjectTeachers.TryGetValue(entry.Key, out var teachers))
                {
                    continue;
                }

                options += teachers.Count(x => !busySet.Contains(x));
            }

            return options;
        }

        private static bool TryPickAssignment(
            int classId,
            DayOfWeek day,
            int periodIndex,
            IReadOnlyDictionary<int, Dictionary<int, int>> remainingByClass,
            IReadOnlyDictionary<int, List<Guid>> subjectTeachers,
            IReadOnlyDictionary<(DayOfWeek Day, int PeriodIndex), HashSet<Guid>> busyTeachersBySlot,
            Random random,
            out int subjectId,
            out Guid teacherId)
        {
            subjectId = default;
            teacherId = Guid.Empty;

            var slotKey = (day, periodIndex);
            var busySet = busyTeachersBySlot.TryGetValue(slotKey, out var teachersInSlot)
                ? teachersInSlot
                : new HashSet<Guid>();

            var subjectCandidates = remainingByClass[classId]
                .Where(x => x.Value > 0)
                .OrderByDescending(x => x.Value)
                .ThenBy(_ => random.Next())
                .Select(x => x.Key)
                .ToList();

            foreach (var candidateSubjectId in subjectCandidates)
            {
                if (!subjectTeachers.TryGetValue(candidateSubjectId, out var teachers) || teachers.Count == 0)
                {
                    continue;
                }

                var availableTeachers = teachers
                    .Where(x => !busySet.Contains(x))
                    .OrderBy(_ => random.Next())
                    .ToList();

                if (availableTeachers.Count == 0)
                {
                    continue;
                }

                subjectId = candidateSubjectId;
                teacherId = availableTeachers[0];
                return true;
            }

            return false;
        }

        private static List<Attendance> BuildAttendances(
            IReadOnlyCollection<Student> students,
            IReadOnlyCollection<Timetable> timetables,
            SeederSettings settings,
            Random random)
        {
            var studentsByClass = students
                .GroupBy(x => x.ClassId)
                .ToDictionary(x => x.Key, x => x.ToList());

            var attendances = new List<Attendance>();
            var weekStart = settings.AttendanceWeekStart.Date;

            foreach (var row in timetables)
            {
                if (!studentsByClass.TryGetValue(row.ClassId, out var classStudents))
                {
                    continue;
                }

                var date = weekStart.AddDays(ToWeekdayOffset(row.DayOfWeek));
                foreach (var student in classStudents)
                {
                    var roll = random.NextDouble();
                    var status = roll switch
                    {
                        < 0.88 => AttendanceStatus.Present,
                        < 0.95 => AttendanceStatus.Late,
                        _ => AttendanceStatus.Absent
                    };

                    attendances.Add(new Attendance
                    {
                        StudentId = student.Id,
                        TimetableId = row.Id,
                        Date = date,
                        Status = status,
                        Note = status switch
                        {
                            AttendanceStatus.Absent => "Vang mat",
                            AttendanceStatus.Late => "Di hoc muon",
                            _ => null
                        }
                    });
                }
            }

            return attendances;
        }

        private static List<Score> BuildScores(
            IReadOnlyCollection<Student> students,
            IReadOnlyList<Semester> semesters,
            IReadOnlyCollection<Timetable> timetables,
            SeederSettings settings,
            Random random)
        {
            var subjectsByClass = timetables
                .GroupBy(x => x.ClassId)
                .ToDictionary(
                    x => x.Key,
                    x => x.Select(y => y.SubjectId).Distinct().OrderBy(y => y).ToList());

            var semesterRanges = BuildSemesterDateRanges(settings, semesters.Count);
            var scores = new List<Score>();

            foreach (var student in students)
            {
                if (!subjectsByClass.TryGetValue(student.ClassId, out var subjectIds))
                {
                    continue;
                }

                for (var semesterIndex = 0; semesterIndex < semesters.Count; semesterIndex++)
                {
                    foreach (var subjectId in subjectIds)
                    {
                        var regular = GenerateScore(random, 4.0, 10.0);
                        var midterm = GenerateScore(random, 3.5, 10.0);
                        var final = GenerateScore(random, 3.0, 10.0);

                        scores.Add(new Score
                        {
                            StudentId = student.Id,
                            SubjectId = subjectId,
                            SemesterId = semesters[semesterIndex].Id,
                            Type = ScoreType.Regular,
                            Value = regular,
                            Grade = ToGrade(regular),
                            CreatedAt = RandomDate(random, semesterRanges[semesterIndex].Start, semesterRanges[semesterIndex].End)
                        });

                        scores.Add(new Score
                        {
                            StudentId = student.Id,
                            SubjectId = subjectId,
                            SemesterId = semesters[semesterIndex].Id,
                            Type = ScoreType.Midterm,
                            Value = midterm,
                            Grade = ToGrade(midterm),
                            CreatedAt = RandomDate(random, semesterRanges[semesterIndex].Start, semesterRanges[semesterIndex].End)
                        });

                        scores.Add(new Score
                        {
                            StudentId = student.Id,
                            SubjectId = subjectId,
                            SemesterId = semesters[semesterIndex].Id,
                            Type = ScoreType.Final,
                            Value = final,
                            Grade = ToGrade(final),
                            CreatedAt = RandomDate(random, semesterRanges[semesterIndex].Start, semesterRanges[semesterIndex].End)
                        });
                    }
                }
            }

            return scores;
        }

        private static List<Conversation> BuildConversations(IReadOnlyCollection<Student> students)
        {
            return students
                .Select(x => new Conversation
                {
                    StudentId = x.Id
                })
                .ToList();
        }

        private static List<Message> BuildMessages(
            IReadOnlyCollection<Conversation> conversations,
            IReadOnlyCollection<Student> students,
            IReadOnlyCollection<StudentParent> studentParents,
            IReadOnlyCollection<Class> classes,
            IReadOnlyCollection<Teacher> teachers,
            Guid adminUserId,
            SeederSettings settings,
            Random random)
        {
            var studentsById = students.ToDictionary(x => x.Id);
            var classesById = classes.ToDictionary(x => x.Id);
            var teachersById = teachers.ToDictionary(x => x.Id);

            var parentLinksByStudentId = studentParents
                .GroupBy(x => x.StudentId)
                .ToDictionary(x => x.Key, x => x.ToList());

            var parentTemplates = new[]
            {
                "Phu huynh xin cap nhat tinh hinh hoc tap cua em {0}.",
                "Phu huynh muon trao doi ve viec di hoc dung gio cua em {0}.",
                "Phu huynh de nghi giao vien ho tro them mon hoc cho em {0}.",
                "Phu huynh xin hen lich trao doi truc tiep ve em {0}."
            };

            var teacherTemplates = new[]
            {
                "Giao vien da ghi nhan thong tin cua em {0} va se theo doi sat hon.",
                "Giao vien se cap nhat ket qua hoc tap cua em {0} vao cuoi tuan.",
                "Nha truong da nhan duoc phan anh, em {0} se duoc ho tro bo sung.",
                "Cam on phu huynh, giao vien chu nhiem da tiep nhan thong tin ve em {0}."
            };

            var messages = new List<Message>();

            foreach (var conversation in conversations)
            {
                if (!studentsById.TryGetValue(conversation.StudentId, out var student))
                {
                    continue;
                }

                if (!classesById.TryGetValue(student.ClassId, out var classroom))
                {
                    continue;
                }

                var teacherUserId = adminUserId;
                if (classroom.HomeroomTeacherId.HasValue &&
                    teachersById.TryGetValue(classroom.HomeroomTeacherId.Value, out var homeroomTeacher))
                {
                    teacherUserId = homeroomTeacher.UserId;
                }

                parentLinksByStudentId.TryGetValue(student.Id, out var parentLinks);

                var messageCount = random.Next(settings.ConversationMessagesMin, settings.ConversationMessagesMax + 1);
                var baseTime = settings.ConversationStartDate.Date
                    .AddDays(random.Next(0, settings.ConversationDaysSpan))
                    .AddHours(7)
                    .AddMinutes(random.Next(0, 180));

                for (var i = 0; i < messageCount; i++)
                {
                    var isParentMessage = i % 2 == 0;
                    Guid senderId;

                    if (isParentMessage)
                    {
                        if (parentLinks == null || parentLinks.Count == 0)
                        {
                            continue;
                        }

                        var parentLink = parentLinks[random.Next(parentLinks.Count)];
                        senderId = parentLink.ParentUserId;
                    }
                    else
                    {
                        senderId = teacherUserId;
                    }

                    var templates = isParentMessage ? parentTemplates : teacherTemplates;
                    var template = templates[random.Next(templates.Length)];

                    messages.Add(new Message
                    {
                        ConversationId = conversation.Id,
                        SenderId = senderId,
                        Content = string.Format(CultureInfo.InvariantCulture, template, student.StudentCode),
                        CreatedAt = baseTime.AddMinutes((i * 35) + random.Next(5, 25))
                    });
                }
            }

            return messages;
        }

        private static List<Pickup> BuildPickups(
            IReadOnlyCollection<Student> students,
            SeederSettings settings,
            Random random)
        {
            var pickups = new List<Pickup>();

            foreach (var student in students)
            {
                for (var i = 0; i < settings.PickupRecordsPerStudent; i++)
                {
                    var date = settings.PickupStartDate.Date.AddDays(random.Next(0, settings.PickupDaysSpan));
                    date = MoveToWeekday(date);

                    var notPicked = random.NextDouble() < settings.PickupNotPickedRate;
                    pickups.Add(new Pickup
                    {
                        StudentId = student.Id,
                        Date = date,
                        Status = notPicked ? "NotPicked" : "Picked",
                        Note = notPicked ? "Phu huynh den tre, nha truong da lien he." : "Ban giao hoc sinh dung quy trinh."
                    });
                }
            }

            return pickups;
        }

        private static List<Meal> BuildMeals(SeederSettings settings)
        {
            var menus = new[]
            {
                "Com ga kho gung, canh cai xanh, chuoi",
                "Com thit kho trung, canh bi do, dua hau",
                "Com suon xao chua ngot, canh rau ngot, tao",
                "Com ca chien sot ca, canh chua, thanh long",
                "Com bo xao hanh tay, canh cai thao, sua chua"
            };

            var dates = BuildSchoolDates(settings.MealStartDate.Date, settings.MealDays);
            return dates
                .Select((date, index) => new Meal
                {
                    Date = date,
                    Menu = menus[index % menus.Length]
                })
                .ToList();
        }

        private static List<StudentMeal> BuildStudentMeals(
            IReadOnlyCollection<Student> students,
            IReadOnlyCollection<Meal> meals,
            SeederSettings settings,
            Random random)
        {
            var studentMeals = new List<StudentMeal>();

            foreach (var meal in meals)
            {
                foreach (var student in students)
                {
                    studentMeals.Add(new StudentMeal
                    {
                        StudentId = student.Id,
                        MealId = meal.Id,
                        IsRegistered = random.NextDouble() < settings.MealRegistrationRate
                    });
                }
            }

            return studentMeals;
        }

        private static List<RefreshToken> BuildRefreshTokens(
            IReadOnlyCollection<User> users,
            SeederSettings settings,
            Random random)
        {
            var tokens = new List<RefreshToken>();
            var usedTokens = new HashSet<string>(StringComparer.Ordinal);

            foreach (var user in users)
            {
                if (random.NextDouble() > settings.RefreshTokenCoverageRate)
                {
                    continue;
                }

                var tokenCount = random.Next(settings.RefreshTokenPerUserMin, settings.RefreshTokenPerUserMax + 1);
                for (var i = 0; i < tokenCount; i++)
                {
                    var issuedAt = DateTime.UtcNow.AddDays(-random.Next(0, settings.RefreshTokenIssueWindowDays + 1));
                    var expiryDate = issuedAt.AddDays(settings.RefreshTokenExpiryDays);
                    var isRevoked = random.NextDouble() < settings.RefreshTokenRevokedRate;

                    var tokenValue = GenerateRefreshTokenValue(user.Id, i, random);
                    while (!usedTokens.Add(tokenValue))
                    {
                        tokenValue = GenerateRefreshTokenValue(user.Id, i + random.Next(1, 1000), random);
                    }

                    tokens.Add(new RefreshToken
                    {
                        Token = tokenValue,
                        UserId = user.Id,
                        ExpiryDate = expiryDate,
                        IsRevoked = isRevoked
                    });
                }
            }

            if (tokens.Count == 0 && users.Count > 0)
            {
                var fallbackUser = users.First();
                tokens.Add(new RefreshToken
                {
                    Token = GenerateRefreshTokenValue(fallbackUser.Id, 1, random),
                    UserId = fallbackUser.Id,
                    ExpiryDate = DateTime.UtcNow.AddDays(settings.RefreshTokenExpiryDays),
                    IsRevoked = false
                });
            }

            return tokens;
        }

        private static string GenerateRefreshTokenValue(Guid userId, int index, Random random)
        {
            var entropy = new byte[16];
            random.NextBytes(entropy);
            return $"seed_{userId:N}_{index}_{Convert.ToHexString(entropy).ToLowerInvariant()}";
        }

        private static DateTime MoveToWeekday(DateTime date)
        {
            while (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                date = date.AddDays(1);
            }

            return date;
        }

        private static List<DateTime> BuildSchoolDates(DateTime startDate, int dayCount)
        {
            var result = new List<DateTime>(dayCount);
            var cursor = startDate;

            while (result.Count < dayCount)
            {
                if (cursor.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
                {
                    result.Add(cursor);
                }

                cursor = cursor.AddDays(1);
            }

            return result;
        }

        private static List<(DateTime Start, DateTime End)> BuildSemesterDateRanges(SeederSettings settings, int semesterCount)
        {
            var startYear = DateTime.UtcNow.Year;
            var endYear = startYear + 1;

            var parts = settings.SchoolYearName
                .Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 &&
                int.TryParse(parts[0], out var parsedStartYear) &&
                int.TryParse(parts[1], out var parsedEndYear))
            {
                startYear = parsedStartYear;
                endYear = parsedEndYear;
            }

            var result = new List<(DateTime Start, DateTime End)>();
            for (var i = 0; i < semesterCount; i++)
            {
                if (i == 0)
                {
                    result.Add((new DateTime(startYear, 9, 1), new DateTime(startYear, 12, 31)));
                }
                else if (i == 1)
                {
                    result.Add((new DateTime(endYear, 1, 1), new DateTime(endYear, 5, 31)));
                }
                else
                {
                    var extraStart = new DateTime(endYear, 6, 1).AddMonths(4 * (i - 2));
                    var extraEnd = extraStart.AddMonths(3).AddDays(-1);
                    result.Add((extraStart, extraEnd));
                }
            }

            return result;
        }

        private static List<SlotTemplate> BuildSlotTemplates(SeederSettings settings)
        {
            var parsedStartTimes = settings.PeriodStartTimes
                .Select(x => TimeSpan.ParseExact(x, @"hh\:mm", CultureInfo.InvariantCulture))
                .ToList();

            var slots = new List<SlotTemplate>();
            for (var dayIndex = 0; dayIndex < settings.PeriodsPerDay.Count; dayIndex++)
            {
                var day = (DayOfWeek)((int)DayOfWeek.Monday + dayIndex);
                for (var periodIndex = 0; periodIndex < settings.PeriodsPerDay[dayIndex]; periodIndex++)
                {
                    var startTime = parsedStartTimes[periodIndex];
                    var endTime = startTime.Add(TimeSpan.FromMinutes(settings.PeriodDurationMinutes));

                    slots.Add(new SlotTemplate(
                        Day: day,
                        PeriodIndex: periodIndex + 1,
                        StartTime: startTime,
                        EndTime: endTime));
                }
            }

            return slots;
        }

        private static int ToWeekdayOffset(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Monday => 0,
                DayOfWeek.Tuesday => 1,
                DayOfWeek.Wednesday => 2,
                DayOfWeek.Thursday => 3,
                DayOfWeek.Friday => 4,
                _ => throw new InvalidOperationException($"Unsupported day for timetable seeding: {dayOfWeek}.")
            };
        }

        private static DateTime RandomDate(Random random, DateTime start, DateTime end)
        {
            if (end <= start)
            {
                return start;
            }

            var range = (end - start).Days;
            return start.AddDays(random.Next(range + 1));
        }

        private static string GenerateStudentName(Random random, bool isMale)
        {
            var lastNames = new[] { "Nguyen", "Tran", "Le", "Pham", "Hoang", "Vu", "Do", "Bui", "Dang", "Phan" };
            var middleNamesMale = new[] { "Van", "Huu", "Minh", "Quoc", "Gia", "Duc" };
            var middleNamesFemale = new[] { "Thi", "Ngoc", "Thanh", "My", "Thu", "Bao" };
            var givenNamesMale = new[] { "An", "Binh", "Cuong", "Dung", "Hieu", "Khanh", "Long", "Nam", "Phuc", "Quan", "Son", "Tuan" };
            var givenNamesFemale = new[] { "Anh", "Chi", "Dung", "Giang", "Hoa", "Huong", "Lan", "Linh", "Mai", "Nga", "Phuong", "Trang" };

            var last = lastNames[random.Next(lastNames.Length)];
            var middle = isMale
                ? middleNamesMale[random.Next(middleNamesMale.Length)]
                : middleNamesFemale[random.Next(middleNamesFemale.Length)];
            var given = isMale
                ? givenNamesMale[random.Next(givenNamesMale.Length)]
                : givenNamesFemale[random.Next(givenNamesFemale.Length)];

            return $"{last} {middle} {given}";
        }

        private static double GenerateScore(Random random, double min, double max)
        {
            return Math.Round(min + (random.NextDouble() * (max - min)), 1);
        }

        private static string ToGrade(double value)
        {
            if (value >= 8.0) return "Gioi";
            if (value >= 6.5) return "Kha";
            if (value >= 5.0) return "Dat";
            return "Can co gang";
        }

        private static User CreateUser(string username, string phoneNumber, string passwordHash)
        {
            return new User
            {
                Username = username,
                Email = $"{username}@demo.local",
                PhoneNumber = phoneNumber,
                PasswordHash = passwordHash,
                IsActive = true
            };
        }

        private static string BuildPhone(string prefix, int number)
        {
            return $"{prefix}{number:D7}";
        }

        private static string BuildTeacherEmail(string fullName, int index)
        {
            var slug = ToAsciiSlug(fullName);
            return $"{slug}.{index:D2}@thptdemo.edu.vn";
        }

        private static string ToAsciiSlug(string input)
        {
            var normalized = input.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var ch in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (category == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (char.IsLetterOrDigit(ch))
                {
                    sb.Append(char.ToLowerInvariant(ch));
                }
                else if (char.IsWhiteSpace(ch) || ch == '-' || ch == '_')
                {
                    sb.Append('.');
                }
            }

            var slug = sb.ToString().Trim('.');
            while (slug.Contains("..", StringComparison.Ordinal))
            {
                slug = slug.Replace("..", ".", StringComparison.Ordinal);
            }

            return string.IsNullOrWhiteSpace(slug) ? "teacher" : slug;
        }

        private static string SanitizeToken(string input)
        {
            var sb = new StringBuilder();
            foreach (var ch in input)
            {
                if (char.IsLetterOrDigit(ch))
                {
                    sb.Append(char.ToLowerInvariant(ch));
                }
            }

            return sb.Length == 0 ? "class" : sb.ToString();
        }

        private static bool SetEquals(IEnumerable<string> left, IEnumerable<string> right)
        {
            var leftSet = left.Select(x => x.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var rightSet = right.Select(x => x.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            return leftSet.SetEquals(rightSet);
        }

        private static string ResolveDataDirectory()
        {
            var candidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "Seeder", "Data"),
                Path.Combine(Directory.GetCurrentDirectory(), "Seeder", "Data"),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Seeder", "Data"))
            };

            foreach (var candidate in candidates)
            {
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }
            }

            throw new DirectoryNotFoundException("Seeder/Data folder was not found. Please check JSON seed files.");
        }

        private static T LoadJson<T>(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Seed file not found: {path}");
            }

            var json = File.ReadAllText(path);
            var value = JsonSerializer.Deserialize<T>(json, JsonOptions);
            if (value == null)
            {
                throw new InvalidOperationException($"Failed to deserialize JSON file: {path}");
            }

            return value;
        }

        private static void ValidateConfig(
            IReadOnlyCollection<SubjectSeedItem> subjects,
            IReadOnlyCollection<ClassSeedItem> classes,
            SeederSettings settings)
        {
            if (subjects.Count != 12)
            {
                throw new InvalidOperationException("subjects.json must contain exactly 12 subjects.");
            }

            if (subjects.Any(x => string.IsNullOrWhiteSpace(x.Name)))
            {
                throw new InvalidOperationException("subjects.json contains empty subject name.");
            }

            if (subjects.Select(x => x.Name.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != subjects.Count)
            {
                throw new InvalidOperationException("subjects.json contains duplicate subject names.");
            }

            if (classes.Count != 3)
            {
                throw new InvalidOperationException("classes.json must contain exactly 3 classes.");
            }

            if (classes.Any(x => string.IsNullOrWhiteSpace(x.Name)))
            {
                throw new InvalidOperationException("classes.json contains empty class name.");
            }

            if (classes.Select(x => x.Name.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != classes.Count)
            {
                throw new InvalidOperationException("classes.json contains duplicate class names.");
            }

            if (classes.Any(x => x.StudentCount < 15 || x.StudentCount > 20))
            {
                throw new InvalidOperationException("Each class in classes.json must have studentCount between 15 and 20.");
            }

            if (string.IsNullOrWhiteSpace(settings.DefaultPasswordHash))
            {
                throw new InvalidOperationException("seeder-settings.json is missing defaultPasswordHash.");
            }

            if (string.IsNullOrWhiteSpace(settings.SchoolYearName))
            {
                throw new InvalidOperationException("seeder-settings.json is missing schoolYearName.");
            }

            if (settings.SemesterNames.Count != 2)
            {
                throw new InvalidOperationException("seeder-settings.json must define exactly 2 semesters.");
            }

            if (settings.TeacherNames.Count < classes.Count || settings.TeacherNames.Count < 3)
            {
                throw new InvalidOperationException("seeder-settings.json must define at least 3 teacher names.");
            }

            if (settings.TeacherNames.Any(x => string.IsNullOrWhiteSpace(x)))
            {
                throw new InvalidOperationException("seeder-settings.json contains empty teacher name.");
            }

            if (settings.TeacherNames.Distinct(StringComparer.OrdinalIgnoreCase).Count() != settings.TeacherNames.Count)
            {
                throw new InvalidOperationException("seeder-settings.json contains duplicate teacher names.");
            }

            if (settings.PeriodsPerDay.Count != 5)
            {
                throw new InvalidOperationException("periodsPerDay must have exactly 5 values (Monday to Friday).");
            }

            if (settings.PeriodsPerDay.Any(x => x < 5 || x > 6))
            {
                throw new InvalidOperationException("Each value in periodsPerDay must be 5 or 6.");
            }

            if (settings.PeriodStartTimes.Count < settings.PeriodsPerDay.Max())
            {
                throw new InvalidOperationException("periodStartTimes must have at least as many entries as max periods in a day.");
            }

            if (settings.PeriodDurationMinutes < 30 || settings.PeriodDurationMinutes > 60)
            {
                throw new InvalidOperationException("periodDurationMinutes should be between 30 and 60.");
            }

            var subjectNames = subjects.Select(x => x.Name.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var frequencyNames = settings.WeeklySubjectFrequency.Keys
                .Select(x => x.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (!subjectNames.SetEquals(frequencyNames))
            {
                throw new InvalidOperationException("weeklySubjectFrequency must contain all and only subjects from subjects.json.");
            }

            if (settings.WeeklySubjectFrequency.Values.Any(x => x <= 0))
            {
                throw new InvalidOperationException("weeklySubjectFrequency values must be greater than 0.");
            }

            var weeklyPeriodCount = settings.PeriodsPerDay.Sum();
            if (settings.WeeklySubjectFrequency.Values.Sum() != weeklyPeriodCount)
            {
                throw new InvalidOperationException(
                    $"weeklySubjectFrequency total ({settings.WeeklySubjectFrequency.Values.Sum()}) must equal total weekly periods ({weeklyPeriodCount}).");
            }

            if (settings.ConversationMessagesMin < 1 || settings.ConversationMessagesMax < settings.ConversationMessagesMin)
            {
                throw new InvalidOperationException("Conversation message range is invalid.");
            }

            if (settings.ConversationDaysSpan < 1)
            {
                throw new InvalidOperationException("conversationDaysSpan must be at least 1.");
            }

            if (settings.PickupRecordsPerStudent < 1 || settings.PickupDaysSpan < 1)
            {
                throw new InvalidOperationException("Pickup settings are invalid.");
            }

            if (settings.PickupNotPickedRate is < 0 or > 1)
            {
                throw new InvalidOperationException("pickupNotPickedRate must be between 0 and 1.");
            }

            if (settings.MealDays < 1)
            {
                throw new InvalidOperationException("mealDays must be at least 1.");
            }

            if (settings.MealRegistrationRate is < 0 or > 1)
            {
                throw new InvalidOperationException("mealRegistrationRate must be between 0 and 1.");
            }

            if (settings.RefreshTokenCoverageRate is < 0 or > 1)
            {
                throw new InvalidOperationException("refreshTokenCoverageRate must be between 0 and 1.");
            }

            if (settings.RefreshTokenRevokedRate is < 0 or > 1)
            {
                throw new InvalidOperationException("refreshTokenRevokedRate must be between 0 and 1.");
            }

            if (settings.RefreshTokenPerUserMin < 1 || settings.RefreshTokenPerUserMax < settings.RefreshTokenPerUserMin)
            {
                throw new InvalidOperationException("RefreshToken per-user range is invalid.");
            }

            if (settings.RefreshTokenIssueWindowDays < 1 || settings.RefreshTokenExpiryDays < 1)
            {
                throw new InvalidOperationException("RefreshToken day settings must be at least 1.");
            }
        }

        private sealed record SlotTemplate(
            DayOfWeek Day,
            int PeriodIndex,
            TimeSpan StartTime,
            TimeSpan EndTime);

        private sealed class SubjectSeedItem
        {
            public string Name { get; set; } = string.Empty;
        }

        private sealed class ClassSeedItem
        {
            public string Name { get; set; } = string.Empty;
            public int StudentCount { get; set; }
            public string? HomeroomTeacher { get; set; }
        }

        private sealed class SeederSettings
        {
            public bool ForceReseedOnStartup { get; set; } = true;
            public int RandomSeed { get; set; } = 20260319;
            public string DefaultPasswordHash { get; set; } = string.Empty;
            public string SchoolYearName { get; set; } = "2025-2026";
            public List<string> SemesterNames { get; set; } = [];
            public List<string> TeacherNames { get; set; } = [];
            public int TeachersPerSubject { get; set; } = 3;
            public List<int> PeriodsPerDay { get; set; } = [];
            public List<string> PeriodStartTimes { get; set; } = [];
            public int PeriodDurationMinutes { get; set; } = 45;
            public DateTime AttendanceWeekStart { get; set; } = new(2026, 3, 16);
            public Dictionary<string, int> WeeklySubjectFrequency { get; set; } = new(StringComparer.OrdinalIgnoreCase);

            public int ConversationMessagesMin { get; set; } = 4;
            public int ConversationMessagesMax { get; set; } = 8;
            public DateTime ConversationStartDate { get; set; } = new(2026, 2, 17);
            public int ConversationDaysSpan { get; set; } = 35;

            public int PickupRecordsPerStudent { get; set; } = 2;
            public DateTime PickupStartDate { get; set; } = new(2026, 3, 10);
            public int PickupDaysSpan { get; set; } = 20;
            public double PickupNotPickedRate { get; set; } = 0.12;

            public int MealDays { get; set; } = 10;
            public DateTime MealStartDate { get; set; } = new(2026, 3, 16);
            public double MealRegistrationRate { get; set; } = 0.82;

            public double RefreshTokenCoverageRate { get; set; } = 0.65;
            public double RefreshTokenRevokedRate { get; set; } = 0.18;
            public int RefreshTokenPerUserMin { get; set; } = 1;
            public int RefreshTokenPerUserMax { get; set; } = 2;
            public int RefreshTokenExpiryDays { get; set; } = 7;
            public int RefreshTokenIssueWindowDays { get; set; } = 14;
        }
    }
}