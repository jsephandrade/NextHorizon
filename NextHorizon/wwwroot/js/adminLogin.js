function showToast(message, isSuccess = true) {
    const toastElement = document.getElementById('monochromeToast');
    const toastBody = document.getElementById('toastMessage');

    toastBody.textContent = message;
    toastElement.classList.remove('hide');
    toastElement.style.backgroundColor = isSuccess ? '#000' : '#dc3545';

    const toast = new bootstrap.Toast(toastElement, {
        animation: true,
        autohide: true,
        delay: 3000
    });

    toast.show();
}

document.getElementById('mainLoginForm').addEventListener('submit', async function (e) {
    e.preventDefault();

    const username = document.getElementById('username').value.trim();
    const password = document.getElementById('password').value;

    if (!username || !password) {
        showToast('Please enter both username and password.', false);
        return;
    }

    const submitBtn = document.querySelector('#mainLoginForm .btn-access');
    submitBtn.disabled = true;
    submitBtn.textContent = 'AUTHENTICATING...';

    try {
        const response = await fetch('/Login/AuthenticateAdmin', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                username,
                password,
                selectedRole: null
            })
        });

        const data = await response.json();

        if (data.success) {
            showToast(data.message, true);
            setTimeout(() => {
                window.location.href = data.redirectUrl;
            }, 1200);
            return;
        }

        showToast(data.message || 'Invalid username or password.', false);
    } catch {
        showToast('Connection error. Please try again.', false);
    } finally {
        submitBtn.disabled = false;
        submitBtn.textContent = 'ACCESS QA';
    }
});

async function sendResetLink() {
    const email = document.getElementById('resetEmail').value.trim();

    if (!email) {
        showToast('Please enter your email address.', false);
        return;
    }

    const resetBtn = document.querySelector('#forgotPasswordModal .btn-access');
    resetBtn.disabled = true;
    resetBtn.textContent = 'SENDING OTP...';

    try {
        const response = await fetch('/Login/GenerateOTP', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email })
        });

        const data = await response.json();
        if (data.success) {
            showToast(data.message, true);
            bootstrap.Modal.getInstance(document.getElementById('forgotPasswordModal')).hide();

            setTimeout(() => {
                sessionStorage.setItem('resetEmail', email);
                new bootstrap.Modal(document.getElementById('otpVerificationModal')).show();
                document.getElementById('otp1').focus();
            }, 350);
            return;
        }

        showToast(data.message || 'Failed to send OTP.', false);
    } catch {
        showToast('Error sending OTP.', false);
    } finally {
        resetBtn.disabled = false;
        resetBtn.textContent = 'SEND OTP';
    }
}

function moveToNext(current, nextId) {
    if (current.value.length === current.maxLength && nextId) {
        document.getElementById(nextId).focus();
    }
}

async function verifyOTP() {
    const email = sessionStorage.getItem('resetEmail');
    const otp = Array.from({ length: 6 }, (_, index) => document.getElementById(`otp${index + 1}`).value).join('');

    if (!email || otp.length !== 6) {
        showToast('Please enter the complete 6-digit OTP.', false);
        return;
    }

    const verifyBtn = document.querySelector('#otpVerificationModal .btn-access');
    verifyBtn.disabled = true;
    verifyBtn.textContent = 'VERIFYING...';

    try {
        const response = await fetch('/Login/VerifyOTP', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, otp })
        });

        const data = await response.json();
        if (data.status === 'Valid') {
            showToast('OTP verified successfully.', true);
            bootstrap.Modal.getInstance(document.getElementById('otpVerificationModal')).hide();

            sessionStorage.setItem('resetToken', data.resetToken);
            setTimeout(() => {
                new bootstrap.Modal(document.getElementById('resetPasswordModal')).show();
            }, 350);
            return;
        }

        showToast(data.message || 'Invalid OTP code.', false);
    } catch {
        showToast('Error verifying OTP.', false);
    } finally {
        verifyBtn.disabled = false;
        verifyBtn.textContent = 'VERIFY OTP';
    }
}

async function resendOTP() {
    const email = sessionStorage.getItem('resetEmail');
    if (!email) {
        showToast('No reset email found.', false);
        return;
    }

    try {
        const response = await fetch('/Login/GenerateOTP', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email })
        });

        const data = await response.json();
        showToast(data.message || 'New OTP sent.', !!data.success);
    } catch {
        showToast('Error resending OTP.', false);
    }
}

async function submitNewPassword() {
    const newPassword = document.getElementById('newPassword').value;
    const confirmPassword = document.getElementById('confirmPassword').value;
    const email = sessionStorage.getItem('resetEmail');
    const resetToken = sessionStorage.getItem('resetToken');

    if (!email || !resetToken) {
        showToast('Reset session expired. Start again.', false);
        return;
    }

    if (!newPassword || newPassword.length < 8) {
        showToast('Password must be at least 8 characters.', false);
        return;
    }

    if (newPassword !== confirmPassword) {
        showToast('Passwords do not match.', false);
        return;
    }

    const resetBtn = document.querySelector('#resetPasswordModal .btn-access');
    resetBtn.disabled = true;
    resetBtn.textContent = 'RESETTING...';

    try {
        const response = await fetch('/Login/ResetPassword', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                email,
                resetToken,
                newPassword,
                confirmPassword
            })
        });

        const data = await response.json();
        if (data.status === 'Success') {
            showToast('Password reset successfully. You can now sign in.', true);
            bootstrap.Modal.getInstance(document.getElementById('resetPasswordModal')).hide();
            sessionStorage.removeItem('resetEmail');
            sessionStorage.removeItem('resetToken');
            document.getElementById('newPassword').value = '';
            document.getElementById('confirmPassword').value = '';
            return;
        }

        showToast(data.message || 'Password reset failed.', false);
    } catch {
        showToast('Error resetting password.', false);
    } finally {
        resetBtn.disabled = false;
        resetBtn.textContent = 'RESET PASSWORD';
    }
}

function togglePassword(inputId, button) {
    const input = document.getElementById(inputId);
    if (input.type === 'password') {
        input.type = 'text';
        button.innerHTML = '<i class="bi bi-eye-slash"></i>';
        return;
    }

    input.type = 'password';
    button.innerHTML = '<i class="bi bi-eye"></i>';
}

function updatePasswordStrength(password) {
    const bar = document.getElementById('passwordStrength');
    const text = document.getElementById('passwordStrengthText');

    let score = 0;
    if (password.length >= 8) score += 1;
    if (/[A-Z]/.test(password)) score += 1;
    if (/[a-z]/.test(password)) score += 1;
    if (/\d/.test(password)) score += 1;
    if (/[^A-Za-z0-9]/.test(password)) score += 1;

    const widths = ['0%', '20%', '40%', '60%', '80%', '100%'];
    const labels = ['Enter password', 'Very weak', 'Weak', 'Fair', 'Strong', 'Very strong'];
    const colors = ['#ced4da', '#dc3545', '#fd7e14', '#ffc107', '#20c997', '#198754'];

    bar.style.width = widths[score];
    bar.style.backgroundColor = colors[score];
    text.textContent = labels[score];
}

document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.form-control').forEach(input => {
        input.addEventListener('input', function () {
            this.style.borderColor = this.value.trim() ? '#000' : '#e9ecef';
        });
    });

    const passwordInput = document.getElementById('newPassword');
    if (passwordInput) {
        passwordInput.addEventListener('input', function () {
            updatePasswordStrength(this.value);
        });
    }
});
