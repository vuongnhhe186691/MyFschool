import 'package:flutter/material.dart';
import '../core/app_theme.dart';
import '../services/api_service.dart';

class ForgotPasswordScreen extends StatefulWidget {
  const ForgotPasswordScreen({super.key});

  @override
  State<ForgotPasswordScreen> createState() => _ForgotPasswordScreenState();
}

class _ForgotPasswordScreenState extends State<ForgotPasswordScreen> {
  final _phoneController = TextEditingController();
  final _otpController = TextEditingController();
  final _newPassController = TextEditingController();
  final _confirmPassController = TextEditingController();

  bool _otpSent = false;
  bool _sendingOtp = false;
  bool _resetting = false;
  bool _obscureNew = true;

  @override
  void dispose() {
    _phoneController.dispose();
    _otpController.dispose();
    _newPassController.dispose();
    _confirmPassController.dispose();
    super.dispose();
  }

  Future<void> _sendOtp() async {
    final phone = _phoneController.text.trim();
    if (phone.isEmpty) {
      _showMsg('Vui lòng nhập số điện thoại', isError: true);
      return;
    }

    setState(() => _sendingOtp = true);
    try {
      final msg = await ApiService.instance.requestForgotPasswordOtp(phone);
      if (!mounted) return;
      setState(() => _otpSent = true);
      _showMsg(msg);
    } catch (e) {
      if (mounted) _showMsg(e.toString(), isError: true);
    } finally {
      if (mounted) setState(() => _sendingOtp = false);
    }
  }

  Future<void> _resetPassword() async {
    final phone = _phoneController.text.trim();
    final otp = _otpController.text.trim();
    final newPass = _newPassController.text;
    final confirmPass = _confirmPassController.text;

    if (phone.isEmpty || otp.isEmpty || newPass.isEmpty) {
      _showMsg('Vui lòng nhập đầy đủ SĐT, OTP và Mật khẩu mới', isError: true);
      return;
    }
    if (newPass != confirmPass) {
      _showMsg('Mật khẩu xác nhận không khớp', isError: true);
      return;
    }

    setState(() => _resetting = true);
    try {
      final msg = await ApiService.instance.resetPassword(
        phoneNumber: phone,
        otpCode: otp,
        newPassword: newPass,
      );
      if (!mounted) return;
      _showMsg(msg);
      Navigator.of(context).pop();
    } catch (e) {
      if (mounted) _showMsg(e.toString(), isError: true);
    } finally {
      if (mounted) setState(() => _resetting = false);
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
      appBar: AppBar(
        title: AnimatedSwitcher(
          duration: const Duration(milliseconds: 220),
          child: Text(
            _otpSent ? 'Reset Password' : 'Forgot Password',
            key: ValueKey(_otpSent),
          ),
        ),
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(20),
        child: Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 460),
            child: Column(
              children: [
                FadeSlideIn(
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
                              width: 54,
                              height: 54,
                              decoration: BoxDecoration(
                                color: AppColors.orangeLight,
                                borderRadius: BorderRadius.circular(16),
                              ),
                              child: const Icon(
                                Icons.lock_reset_rounded,
                                color: AppColors.orange,
                                size: 32,
                              ),
                            ),
                            const SizedBox(width: 14),
                            const Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(
                                    'Khôi phục mật khẩu',
                                    style: TextStyle(
                                      fontSize: 17,
                                      fontWeight: FontWeight.w700,
                                      color: AppColors.textDark,
                                    ),
                                  ),
                                  SizedBox(height: 2),
                                  Text(
                                    'Nhận mã OTP qua email đã đăng ký',
                                    style: TextStyle(
                                      fontSize: 12.5,
                                      color: AppColors.textMuted,
                                    ),
                                  ),
                                ],
                              ),
                            ),
                          ],
                        ),
                        const SizedBox(height: 18),
                        TextField(
                          controller: _phoneController,
                          keyboardType: TextInputType.phone,
                          decoration: AppTheme.modernInput(
                            label: 'Số điện thoại',
                            hint: 'Nhập số điện thoại tài khoản',
                            prefixIcon: Icons.phone_android_rounded,
                          ),
                        ),
                        const SizedBox(height: 16),
                        GradientButton(
                          label: 'Gửi mã OTP',
                          icon: Icons.send_rounded,
                          isLoading: _sendingOtp,
                          onPressed: _sendOtp,
                        ),
                      ],
                    ),
                  ),
                ),

                if (!_otpSent) ...[
                  const SizedBox(height: 10),
                  TextButton(
                    onPressed: () => setState(() => _otpSent = true),
                    child: const Text(
                      'Đã có mã OTP? Nhập OTP ngay',
                      style: TextStyle(
                        color: AppColors.orange,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ),
                ],

                AnimatedSize(
                  duration: const Duration(milliseconds: 350),
                  curve: Curves.easeOutCubic,
                  child: !_otpSent
                      ? const SizedBox.shrink()
                      : Padding(
                          padding: const EdgeInsets.only(top: 18),
                          child: FadeSlideIn(
                            child: Container(
                              padding: const EdgeInsets.all(22),
                              decoration: BoxDecoration(
                                color: Colors.white,
                                borderRadius: BorderRadius.circular(24),
                                border: Border.all(
                                  color: AppColors.orange.withValues(alpha: 0.3),
                                ),
                                boxShadow: AppColors.softShadow(),
                              ),
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.stretch,
                                children: [
                                  const Text(
                                    'Nhập OTP & Mật khẩu mới',
                                    style: TextStyle(
                                      fontSize: 16,
                                      fontWeight: FontWeight.w700,
                                      color: AppColors.textDark,
                                    ),
                                  ),
                                  const SizedBox(height: 14),
                                  TextField(
                                    controller: _otpController,
                                    keyboardType: TextInputType.number,
                                    decoration: AppTheme.modernInput(
                                      label: 'Mã OTP',
                                      hint: 'Nhập 6 chữ số OTP',
                                      prefixIcon: Icons.password_rounded,
                                    ),
                                  ),
                                  const SizedBox(height: 12),
                                  TextField(
                                    controller: _newPassController,
                                    obscureText: _obscureNew,
                                    decoration: AppTheme.modernInput(
                                      label: 'Mật khẩu mới',
                                      hint: 'Nhập mật khẩu mới',
                                      prefixIcon: Icons.lock_rounded,
                                      suffixIcon: IconButton(
                                        icon: Icon(
                                          _obscureNew
                                              ? Icons.visibility_off_rounded
                                              : Icons.visibility_rounded,
                                          color: AppColors.orange,
                                        ),
                                        onPressed: () => setState(
                                          () => _obscureNew = !_obscureNew,
                                        ),
                                      ),
                                    ),
                                  ),
                                  const SizedBox(height: 12),
                                  TextField(
                                    controller: _confirmPassController,
                                    obscureText: _obscureNew,
                                    decoration: AppTheme.modernInput(
                                      label: 'Xác nhận mật khẩu',
                                      hint: 'Nhập lại mật khẩu mới',
                                      prefixIcon: Icons.lock_outline_rounded,
                                    ),
                                  ),
                                  const SizedBox(height: 18),
                                  GradientButton(
                                    label: 'Cập nhật mật khẩu',
                                    icon: Icons.check_circle_outline_rounded,
                                    isLoading: _resetting,
                                    onPressed: _resetPassword,
                                  ),
                                ],
                              ),
                            ),
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
