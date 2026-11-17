import 'package:flutter/material.dart';
import '../core/app_theme.dart';
import '../models/models.dart';
import '../services/api_service.dart';

class ProfileScreen extends StatefulWidget {
  const ProfileScreen({super.key});

  @override
  State<ProfileScreen> createState() => _ProfileScreenState();
}

class _ProfileScreenState extends State<ProfileScreen> {
  final _usernameCtrl = TextEditingController();
  final _emailCtrl = TextEditingController();
  final _phoneCtrl = TextEditingController();

  ProfileData? _profile;
  bool _loading = true;
  bool _saving = false;
  bool _twoFactorEnabled = false;

  @override
  void initState() {
    super.initState();
    _loadProfile();
  }

  @override
  void dispose() {
    _usernameCtrl.dispose();
    _emailCtrl.dispose();
    _phoneCtrl.dispose();
    super.dispose();
  }

  Future<void> _loadProfile() async {
    setState(() => _loading = true);
    try {
      final profile = await ApiService.instance.getMyProfile();
      if (!mounted) return;
      setState(() {
        _profile = profile;
        _usernameCtrl.text = profile.username;
        _emailCtrl.text = profile.email;
        _phoneCtrl.text = profile.phoneNumber;
        _twoFactorEnabled = profile.isTwoFactorEnabled;
        _loading = false;
      });
    } catch (e) {
      if (mounted) {
        setState(() => _loading = false);
        _showMsg(e.toString(), isError: true);
      }
    }
  }

  Future<void> _saveProfile() async {
    setState(() => _saving = true);
    try {
      final msg = await ApiService.instance.updateMyProfile(
        username: _usernameCtrl.text,
        email: _emailCtrl.text,
        phoneNumber: _phoneCtrl.text,
      );
      if (!mounted) return;
      _showMsg(msg);
      await _loadProfile();
    } catch (e) {
      if (mounted) _showMsg(e.toString(), isError: true);
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  Future<void> _toggleTwoFactor(bool value) async {
    try {
      final msg = await ApiService.instance.updateTwoFactor(value);
      if (!mounted) return;
      setState(() => _twoFactorEnabled = value);
      _showMsg(msg);
    } catch (e) {
      if (mounted) _showMsg(e.toString(), isError: true);
    }
  }

  void _showMsg(String msg, {bool isError = false}) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(msg),
        backgroundColor: isError ? AppColors.redFail : AppColors.greenPass,
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('My Profile')),
      body: _loading
          ? const Center(
              child: CircularProgressIndicator(color: AppColors.orange),
            )
          : SingleChildScrollView(
              padding: const EdgeInsets.all(20),
              child: Center(
                child: ConstrainedBox(
                  constraints: const BoxConstraints(maxWidth: 460),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      FadeSlideIn(
                        child: Container(
                          padding: const EdgeInsets.all(20),
                          decoration: BoxDecoration(
                            gradient: AppColors.primaryGradient,
                            borderRadius: BorderRadius.circular(24),
                            boxShadow: AppColors.softShadow(tint: AppColors.orange),
                          ),
                          child: Column(
                            children: [
                              Container(
                                width: 76,
                                height: 76,
                                decoration: BoxDecoration(
                                  color: Colors.white,
                                  shape: BoxShape.circle,
                                  boxShadow: [
                                    BoxShadow(
                                      color: Colors.black.withValues(alpha: 0.12),
                                      blurRadius: 12,
                                    ),
                                  ],
                                ),
                                child: const Icon(
                                  Icons.person_rounded,
                                  color: AppColors.orange,
                                  size: 44,
                                ),
                              ),
                              const SizedBox(height: 12),
                              Text(
                                _profile?.username ?? '',
                                style: const TextStyle(
                                  color: Colors.white,
                                  fontSize: 20,
                                  fontWeight: FontWeight.w800,
                                ),
                              ),
                              const SizedBox(height: 6),
                              if (_profile != null)
                                Wrap(
                                  spacing: 6,
                                  children: _profile!.roles
                                      .map(
                                        (r) => Container(
                                          padding: const EdgeInsets.symmetric(
                                            horizontal: 12,
                                            vertical: 4,
                                          ),
                                          decoration: BoxDecoration(
                                            color: Colors.white.withValues(
                                              alpha: 0.22,
                                            ),
                                            borderRadius: BorderRadius.circular(
                                              14,
                                            ),
                                          ),
                                          child: Text(
                                            r,
                                            style: const TextStyle(
                                              color: Colors.white,
                                              fontSize: 12,
                                              fontWeight: FontWeight.w700,
                                            ),
                                          ),
                                        ),
                                      )
                                      .toList(),
                                ),
                            ],
                          ),
                        ),
                      ),
                      const SizedBox(height: 18),
                      FadeSlideIn(
                        delay: const Duration(milliseconds: 90),
                        child: Container(
                          padding: const EdgeInsets.all(20),
                          decoration: BoxDecoration(
                            color: Colors.white,
                            borderRadius: BorderRadius.circular(24),
                            border: Border.all(color: AppColors.borderSubtle),
                            boxShadow: AppColors.softShadow(),
                          ),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.stretch,
                            children: [
                              TextField(
                                controller: _usernameCtrl,
                                decoration: AppTheme.modernInput(
                                  label: 'Username',
                                  hint: 'Username',
                                  prefixIcon: Icons.person_outline_rounded,
                                ),
                              ),
                              const SizedBox(height: 14),
                              TextField(
                                controller: _emailCtrl,
                                keyboardType: TextInputType.emailAddress,
                                decoration: AppTheme.modernInput(
                                  label: 'Email',
                                  hint: 'Email',
                                  prefixIcon: Icons.email_outlined,
                                ),
                              ),
                              const SizedBox(height: 14),
                              TextField(
                                controller: _phoneCtrl,
                                keyboardType: TextInputType.phone,
                                decoration: AppTheme.modernInput(
                                  label: 'Số điện thoại',
                                  hint: 'PhoneNumber',
                                  prefixIcon: Icons.phone_android_rounded,
                                ),
                              ),
                              const SizedBox(height: 16),
                              Container(
                                decoration: BoxDecoration(
                                  color: AppColors.bg,
                                  borderRadius: BorderRadius.circular(14),
                                  border: Border.all(
                                    color: AppColors.borderSubtle,
                                  ),
                                ),
                                child: SwitchListTile(
                                  activeThumbColor: AppColors.orange,
                                  title: const Text(
                                    'Xác thực 2 bước (OTP Email)',
                                    style: TextStyle(
                                      fontWeight: FontWeight.w600,
                                      fontSize: 14.5,
                                    ),
                                  ),
                                  subtitle: const Text(
                                    'Yêu cầu nhập mã OTP gửi về email khi đăng nhập',
                                    style: TextStyle(fontSize: 12),
                                  ),
                                  value: _twoFactorEnabled,
                                  onChanged: _toggleTwoFactor,
                                ),
                              ),
                              const SizedBox(height: 20),
                              GradientButton(
                                label: 'Update Profile',
                                icon: Icons.save_rounded,
                                isLoading: _saving,
                                onPressed: _saveProfile,
                              ),
                            ],
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),
    );
  }
}
