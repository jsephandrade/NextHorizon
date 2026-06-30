﻿// Show toast message
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

// Handle login form submission - NO ROLE SELECTION NEEDED
document.getElementById('mainLoginForm').addEventListener('submit', async function(e) {
    e.preventDefault();
    
    const username = document.querySelector('input[type="text"]').value;
    const password = document.getElementById('adminPassword').value;
    
    if (!username || !password) {
        showToast('Please enter both username and password', false);
        return;
    }
    
    const submitBtn = document.querySelector('.btn-access');
    submitBtn.disabled = true;
    submitBtn.textContent = 'AUTHENTICATING...';
    
    try {
        const response = await fetch('/Login/AuthenticateAdmin', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                username: username,
                password: password,
                selectedRole: null  // No role selected - will be detected from database
            })
        });
        
        const data = await response.json();
        
        if (data.success) {
            showToast(data.message, true);
            setTimeout(() => {
                window.location.href = data.redirectUrl;
            }, 1500);
        } else {
            showToast(data.message, false);
            submitBtn.disabled = false;
            submitBtn.textContent = 'ACCESS PORTAL';
        }
    } catch (error) {
        showToast('Connection error. Please try again.', false);
        submitBtn.disabled = false;
        submitBtn.textContent = 'ACCESS PORTAL';
    }
});

// Forgot Password Flow - Step 1: Send OTP
async function sendResetLink() {
    const email = document.getElementById('resetEmail').value;
    
    if (!email) {
        showToast('Please enter your email address', false);
        return;
    }
    
    const resetBtn = document.querySelector('#forgotPasswordModal .btn-access');
    resetBtn.disabled = true;
    resetBtn.textContent = 'SENDING OTP...';
    
    try {
        const response = await fetch('/Login/GenerateOTP', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email: email })
        });
        
        const data = await response.json();
        
        if (data.success) {
            showToast(data.message, true);
            
            // Close forgot password modal
            const forgotModal = bootstrap.Modal.getInstance(document.getElementById('forgotPasswordModal'));
            forgotModal.hide();
            
            // Open OTP verification modal
            setTimeout(() => {
                openOTPModal(email);
            }, 500);
        } else {
            showToast(data.message, false);
            resetBtn.disabled = false;
            resetBtn.textContent = 'SEND OTP';
        }
    } catch (error) {
        showToast('Error sending OTP', false);
        resetBtn.disabled = false;
        resetBtn.textContent = 'SEND OTP';
    }
}

// Open OTP Verification Modal
function openOTPModal(email) {
    // Store email for later use
    sessionStorage.setItem('resetEmail', email);
    
    // Check if modal already exists
    let otpModal = document.getElementById('otpVerificationModal');
    
    if (!otpModal) {
        // Create modal HTML
        const modalHtml = `
            <div class="modal fade" id="otpVerificationModal" tabindex="-1" aria-hidden="true">
                <div class="modal-dialog modal-dialog-centered">
                    <div class="modal-content shadow-lg">
                        <div class="modal-header border-0">
                            <h5 class="fw-bold m-0">Verify OTP Code</h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                        </div>
                        <div class="modal-body py-4 text-center">
                            <i class="bi bi-shield-lock-fill fs-1 text-muted mb-3 d-block"></i>
                            <p class="text-muted small px-3">Enter the 6-digit OTP sent to your email.</p>
                            <div class="mt-4 text-start px-3">
                                <label class="form-label">OTP Code</label>
                                <div class="d-flex gap-2 justify-content-between mb-2">
                                    <input type="text" id="otp1" class="form-control text-center" maxlength="1" style="width: 50px;" onkeyup="moveToNext(this, 'otp2')">
                                    <input type="text" id="otp2" class="form-control text-center" maxlength="1" style="width: 50px;" onkeyup="moveToNext(this, 'otp3')">
                                    <input type="text" id="otp3" class="form-control text-center" maxlength="1" style="width: 50px;" onkeyup="moveToNext(this, 'otp4')">
                                    <input type="text" id="otp4" class="form-control text-center" maxlength="1" style="width: 50px;" onkeyup="moveToNext(this, 'otp5')">
                                    <input type="text" id="otp5" class="form-control text-center" maxlength="1" style="width: 50px;" onkeyup="moveToNext(this, 'otp6')">
                                    <input type="text" id="otp6" class="form-control text-center" maxlength="1" style="width: 50px;" onkeyup="moveToNext(this, null)">
                                </div>
                                <div class="mt-2 text-end">
                                    <small class="text-muted">Didn't receive? <a href="#" onclick="resendOTP()">Resend</a></small>
                                </div>
                            </div>
                        </div>
                        <div class="modal-footer border-0 px-4 pb-4">
                            <button type="button" class="btn btn-access m-0" onclick="verifyOTP()">VERIFY OTP</button>
                        </div>
                    </div>
                </div>
            </div>
        `;
        document.body.insertAdjacentHTML('beforeend', modalHtml);
        otpModal = document.getElementById('otpVerificationModal');
    }
    
    // Clear any existing OTP inputs
    for (let i = 1; i <= 6; i++) {
        const input = document.getElementById(`otp${i}`);
        if (input) input.value = '';
    }
    
    const modal = new bootstrap.Modal(otpModal);
    modal.show();
    
    // Focus on first input after modal is shown
    otpModal.addEventListener('shown.bs.modal', function() {
        const firstInput = document.getElementById('otp1');
        if (firstInput) firstInput.focus();
    }, { once: true });
}

// Move to next OTP input
function moveToNext(current, nextId) {
    if (current.value.length >= current.maxLength) {
        if (nextId) {
            document.getElementById(nextId).focus();
        }
    }
}

// Verify OTP
async function verifyOTP() {
    // Get all OTP inputs
    let otp = '';
    for (let i = 1; i <= 6; i++) {
        const input = document.getElementById(`otp${i}`);
        if (!input) {
            showToast('OTP input fields not found', false);
            return;
        }
        otp += input.value;
    }
    
    const email = sessionStorage.getItem('resetEmail');
    
    if (otp.length !== 6) {
        showToast('Please enter a valid 6-digit OTP', false);
        return;
    }
    
    const verifyBtn = document.querySelector('#otpVerificationModal .btn-access');
    if (verifyBtn) {
        verifyBtn.disabled = true;
        verifyBtn.textContent = 'VERIFYING...';
    }
    
    try {
        const response = await fetch('/Login/VerifyOTP', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email: email, otp: otp })
        });
        
        const data = await response.json();
        
        if (data.status === 'Valid') {
            showToast('OTP verified successfully', true);
            
            // Close OTP modal
            const otpModal = bootstrap.Modal.getInstance(document.getElementById('otpVerificationModal'));
            otpModal.hide();
            
            // Open reset password modal
            setTimeout(() => {
                openResetPasswordModal(email, data.resetToken);
            }, 500);
        } else {
            showToast(data.message, false);
            if (verifyBtn) {
                verifyBtn.disabled = false;
                verifyBtn.textContent = 'VERIFY OTP';
            }
        }
    } catch (error) {
        showToast('Error verifying OTP', false);
        if (verifyBtn) {
            verifyBtn.disabled = false;
            verifyBtn.textContent = 'VERIFY OTP';
        }
    }
}

// Resend OTP
async function resendOTP() {
    const email = sessionStorage.getItem('resetEmail');
    
    if (!email) return;
    
    showToast('Sending new OTP...', true);
    
    try {
        const response = await fetch('/Login/GenerateOTP', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email: email })
        });
        
        const data = await response.json();
        
        if (data.success) {
            showToast('New OTP sent to your email', true);
        } else {
            showToast('Failed to resend OTP', false);
        }
    } catch (error) {
        showToast('Error resending OTP', false);
    }
}

// Open Reset Password Modal
function openResetPasswordModal(email, resetToken) {
    sessionStorage.setItem('resetToken', resetToken);
    
    let resetModal = document.getElementById('resetPasswordModal');
    
    if (!resetModal) {
        const modalHtml = `
            <div class="modal fade" id="resetPasswordModal" tabindex="-1" aria-hidden="true">
                <div class="modal-dialog modal-dialog-centered">
                    <div class="modal-content shadow-lg">
                        <div class="modal-header border-0">
                            <h5 class="fw-bold m-0">Reset Password</h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                        </div>
                        <div class="modal-body py-4">
                            <div class="text-center mb-4">
                                <i class="bi bi-key-fill fs-1 text-muted"></i>
                            </div>
                            <div class="mt-2 px-3">
                                <label class="form-label">New Password</label>
                                <input type="password" id="newPassword" class="form-control" placeholder="••••••••">
                                <small class="text-muted">Minimum 8 characters</small>
                            </div>
                            <div class="mt-3 px-3">
                                <label class="form-label">Confirm Password</label>
                                <input type="password" id="confirmPassword" class="form-control" placeholder="••••••••">
                            </div>
                        </div>
                        <div class="modal-footer border-0 px-4 pb-4">
                            <button type="button" class="btn btn-access m-0" onclick="submitNewPassword()">RESET PASSWORD</button>
                        </div>
                    </div>
                </div>
            </div>
        `;
        document.body.insertAdjacentHTML('beforeend', modalHtml);
        resetModal = document.getElementById('resetPasswordModal');
    }
    
    const modal = new bootstrap.Modal(resetModal);
    modal.show();
}

// Submit New Password
async function submitNewPassword() {
    const newPassword = document.getElementById('newPassword').value;
    const confirmPassword = document.getElementById('confirmPassword').value;
    const email = sessionStorage.getItem('resetEmail');
    const resetToken = sessionStorage.getItem('resetToken');
    
    if (!newPassword || newPassword.length < 8) {
        showToast('Password must be at least 8 characters', false);
        return;
    }
    
    if (newPassword !== confirmPassword) {
        showToast('Passwords do not match', false);
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
                email: email, 
                resetToken: resetToken,
                newPassword: newPassword,
                confirmPassword: confirmPassword
            })
        });
        
        const data = await response.json();
        
        if (data.status === 'Success') {
            showToast('Password reset successfully! You can now login.', true);
            
            // Close modal
            const resetModal = bootstrap.Modal.getInstance(document.getElementById('resetPasswordModal'));
            resetModal.hide();
            
            // Clear session storage
            sessionStorage.removeItem('resetEmail');
            sessionStorage.removeItem('resetToken');
        } else {
            showToast(data.message, false);
            resetBtn.disabled = false;
            resetBtn.textContent = 'RESET PASSWORD';
        }
    } catch (error) {
        showToast('Error resetting password', false);
        resetBtn.disabled = false;
        resetBtn.textContent = 'RESET PASSWORD';
    }
}

// REMOVED: validateUserRole function - no longer needed

// Toggle password visibility
function togglePassword(inputId, button) {
    const input = document.getElementById(inputId);
    if (input.type === 'password') {
        input.type = 'text';
        button.innerHTML = '<i class="bi bi-eye-slash"></i>';
    } else {
        input.type = 'password';
        button.innerHTML = '<i class="bi bi-eye"></i>';
    }
}

// Add input validation styling
document.addEventListener('DOMContentLoaded', function() {
    document.querySelectorAll('.form-control').forEach(input => {
        input.addEventListener('input', function() {
            if (this.value.trim() !== '') {
                this.style.borderColor = '#000';
            } else {
                this.style.borderColor = '#e9ecef';
            }
        });
    });
});

// Handle Enter key press
document.addEventListener('keypress', function(e) {
    if (e.key === 'Enter') {
        e.preventDefault();
        const loginForm = document.getElementById('mainLoginForm');
        if (loginForm) {
            loginForm.dispatchEvent(new Event('submit'));
        }
    }
});
