using Microsoft.EntityFrameworkCore;

namespace PRM_PRJ.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        public DbSet<Student> Students { get; set; }

        public DbSet<StudentParent> StudentParents { get; set; }
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<Class> Classes { get; set; }

        public DbSet<Subject> Subjects { get; set; }
        public DbSet<TeacherSubject> TeacherSubjects { get; set; }

        public DbSet<Timetable> Timetables { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Application> Applications { get; set; }
        public DbSet<Score> Scores { get; set; }
        public DbSet<Semester> Semesters { get; set; }
        public DbSet<SchoolYear> SchoolYears { get; set; }

        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<Message> Messages { get; set; }

        public DbSet<Pickup> Pickups { get; set; }

        public DbSet<Meal> Meals { get; set; }
        public DbSet<StudentMeal> StudentMeals { get; set; }

        public DbSet<AdminNotification> AdminNotifications { get; set; }
        public DbSet<UserNotificationRead> UserNotificationReads { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(x => x.PhoneNumber)
                .IsUnique();

            modelBuilder.Entity<UserRole>()
                .HasKey(x => new { x.UserId, x.RoleId });

            modelBuilder.Entity<UserRole>()
                .HasOne(x => x.User)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserRole>()
                .HasOne(x => x.Role)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RefreshToken>()
                .HasIndex(x => x.Token)
                .IsUnique();

            modelBuilder.Entity<RefreshToken>()
                .HasOne(x => x.User)
                .WithMany(x => x.RefreshTokens)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TeacherSubject>()
                .HasKey(x => new { x.TeacherId, x.SubjectId });

            modelBuilder.Entity<TeacherSubject>()
                .HasOne(x => x.Teacher)
                .WithMany(x => x.TeacherSubjects)
                .HasForeignKey(x => x.TeacherId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TeacherSubject>()
                .HasOne(x => x.Subject)
                .WithMany(x => x.TeacherSubjects)
                .HasForeignKey(x => x.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Class>()
                .HasOne(c => c.HomeroomTeacher)
                .WithMany()
                .HasForeignKey(c => c.HomeroomTeacherId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Student>()
                .HasOne(s => s.Class)
                .WithMany(c => c.Students)
                .HasForeignKey(s => s.ClassId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Student>()
    .HasOne(s => s.User)
    .WithMany()
    .HasForeignKey(s => s.UserId)
    .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentParent>()
     .HasKey(x => new { x.StudentId, x.ParentUserId });

            modelBuilder.Entity<StudentParent>()
                .HasOne(x => x.Student)
                .WithMany(x => x.StudentParents)
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StudentParent>()
                .HasOne(x => x.ParentUser)
                .WithMany(x => x.ParentStudents)
                .HasForeignKey(x => x.ParentUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Timetable>()
                .HasOne(t => t.Class)
                .WithMany()
                .HasForeignKey(t => t.ClassId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Timetable>()
                .HasOne(t => t.Teacher)
                .WithMany()
                .HasForeignKey(t => t.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Timetable>()
                .HasOne(t => t.Subject)
                .WithMany()
                .HasForeignKey(t => t.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Student)
                .WithMany(s => s.Attendances)
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Timetable)
                .WithMany(t => t.Attendances)
                .HasForeignKey(a => a.TimetableId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Score>()
                .HasOne(s => s.Student)
                .WithMany(st => st.Scores)
                .HasForeignKey(s => s.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Score>()
                .HasOne(s => s.Subject)
                .WithMany()
                .HasForeignKey(s => s.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Score>()
                .HasOne(s => s.Semester)
                .WithMany(se => se.Scores)
                .HasForeignKey(s => s.SemesterId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Pickup>()
                .HasOne(p => p.Student)
                .WithMany()
                .HasForeignKey(p => p.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StudentMeal>()
                .HasOne(sm => sm.Student)
                .WithMany()
                .HasForeignKey(sm => sm.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StudentMeal>()
                .HasOne(sm => sm.Meal)
                .WithMany(m => m.StudentMeals)
                .HasForeignKey(sm => sm.MealId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AdminNotification>()
    .HasOne(x => x.CreatedByUser)
    .WithMany()
    .HasForeignKey(x => x.CreatedByUserId)
    .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserNotificationRead>()
                .HasKey(x => new { x.NotificationId, x.UserId });

            modelBuilder.Entity<UserNotificationRead>()
                .HasOne(x => x.Notification)
                .WithMany(x => x.UserReads)
                .HasForeignKey(x => x.NotificationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserNotificationRead>()
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
    .HasIndex(x => x.PhoneNumber)
    .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(x => x.Email)
                .IsUnique();

            modelBuilder.Entity<Application>()
    .HasOne(x => x.SenderUser)
    .WithMany()
    .HasForeignKey(x => x.SenderUserId)
    .OnDelete(DeleteBehavior.Restrict);
        }
    }
}