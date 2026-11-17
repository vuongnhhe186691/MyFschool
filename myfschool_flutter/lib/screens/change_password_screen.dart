import 'package:flutter/material.dart';
import '../core/app_theme.dart';
import '../services/api_service.dart';

class ChangePasswordScreen extends StatefulWidget {
  const ChangePasswordScreen({super.key});

  @override
  State<ChangePasswordScreen> createState() => _ChangePasswordScreenState();
}

class _ChangePasswordScreenState extends State<ChangePasswordScreen> {
  final _currentCtrl = TextEditingController();
  final _newCtrl = TextEditingController();
  final _confirmCtrl = TextEditingController();
  bool _isLoading = false;
  bool _obscure = true;

  @override
  void dispose() {
    _currentCtrl.dispose();
    _newCtrl.dispose();
    _confirmCtrl.dispose();
    super.dispose();
  }

  Future<void> _updatePassword() async {
    final current = _currentCtrl.text;
    final next = _newCtrl.text;
    final confirm = _confirmCtrl.text;

    if (current.isEmpty || next.isEmpty || confirm.isEmpty) {
      _showMsg('Vui lòng điền đầy đủ thông tin', isError: true);
      return;
    }
    if (next != confirm) {
      _showMsg('Mật khẩu xác nhận không khớp', isError: true);
      return;
    }

    setState(() => _isLoading = true);
    try {
      final msg = await ApiService.instance.changePassword(
        currentPassword: current,
        newPassword: next,
      );
      if (!mounted) return;
      _showMsg(msg);
      Navigator.of(context).pop();
    } catch (e) {
      if (mounted) _showMsg(e.toString(), isError: true);
    } finally {
      if (mounted) setState(() => _isLoading = false);
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
      appBar: AppBar(title: const Text('Change Password')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(20),
        child: Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 440),
            child: FadeSlideIn(
              child: Container(
                padding: const EdgeInsets.all(22),
                decoration: BoxDecoration(
                  color: Colors.white,
                  borderRadius: BorderRadius.circular(24),
                  border: Border.all(color: AppColors.borderSubtle),
                  boxShadow: AppColors.softShadow(),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Row(
                      children: [
                        Container(
                          width: 48,
                          height: 48,
                          decoration: BoxDecoration(
                            color: AppColors.orangeLight,
                            borderRadius: BorderRadius.circular(14),
                          ),
                          child: const Icon(
                            Icons.shield_rounded,
                            color: AppColors.orange,
                          ),
                        ),
                        const SizedBox(width: 12),
                        const Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                'Bảo mật tài khoản',
                                style: TextStyle(
                                  fontSize: 16.5,
                                  fontWeight: FontWeight.w700,
                                ),
                              ),
                              Text(
                                'Cập nhật mật khẩu mới cho tài khoản của bạn',
                                style: TextStyle(
                                  fontSize: 12,
                                  color: AppColors.textMuted,
                                ),
                              ),
                            ],
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 20),
                    TextField(
                      controller: _currentCtrl,
                      obscureText: _obscure,
                      decoration: AppTheme.modernInput(
                        label: 'Mật khẩu hiện tại',
                        hint: 'Current password',
                        prefixIcon: Icons.lock_outline_rounded,
                        suffixIcon: IconButton(
                          icon: Icon(
                            _obscure
                                ? Icons.visibility_off_rounded
                                : Icons.visibility_rounded,
                            color: AppColors.orange,
                          ),
                          onPressed: () => setState(() => _obscure = !_obscure),
                        ),
                      ),
                    ),
                    const SizedBox(height: 14),
                    TextField(
                      controller: _newCtrl,
                      obscureText: _obscure,
                      decoration: AppTheme.modernInput(
                        label: 'Mật khẩu mới',
                        hint: 'New password',
                        prefixIcon: Icons.lock_rounded,
                      ),
                    ),
                    const SizedBox(height: 14),
                    TextField(
                      controller: _confirmCtrl,
                      obscureText: _obscure,
                      decoration: AppTheme.modernInput(
                        label: 'Xác nhận mật khẩu',
                        hint: 'Confirm password',
                        prefixIcon: Icons.lock_reset_rounded,
                      ),
                    ),
                    const SizedBox(height: 22),
                    GradientButton(
                      label: 'Update Password',
                      icon: Icons.check_rounded,
                      isLoading: _isLoading,
                      onPressed: _updatePassword,
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
