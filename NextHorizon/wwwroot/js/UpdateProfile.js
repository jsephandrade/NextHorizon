document.addEventListener('DOMContentLoaded', function () {
    console.log('UpdateProfile.js loaded');
    
    let currentField = null;
    let currentRow = null;

    const rowMappings = {
        'name-row': { modal: 'fullnameModal', field: 'fullname' },
        'username-row': { modal: 'universalModal', field: 'username', title: 'USERNAME' },
        'address-row': { modal: 'universalModal', field: 'address', title: 'ADDRESS' },
        'phone-row': { modal: 'universalModal', field: 'phone', title: 'PHONE NUMBER' },
        'email-row': { modal: 'universalModal', field: 'email', title: 'EMAIL ADDRESS' },
        'password-row': { modal: 'passwordModal', field: 'password' }
    };

    // Attach click listeners to rows
    Object.keys(rowMappings).forEach(rowId => {
        const element = document.getElementById(rowId);
        if (element) {
            element.addEventListener('click', function () {
                const config = rowMappings[rowId];
                currentRow = this;
                currentField = config.field;

                console.log('Row clicked:', rowId, config);

                if (config.modal === 'fullnameModal') {
                    openFullNameModal(this);
                } else if (config.modal === 'universalModal') {
                    openUniversalModal(config, this);
                } else if (config.modal === 'passwordModal') {
                    openModal('passwordModal');
                }
            });
        }
    });

    function openFullNameModal(row) {
        const firstNameInput = document.getElementById('firstNameInput');
        const middleNameInput = document.getElementById('middleNameInput');
        const lastNameInput = document.getElementById('lastNameInput');
        
        // Get current values from the row
        const valueSpan = row.querySelector('.info-value');
        const fullNameText = valueSpan ? valueSpan.childNodes[0]?.nodeValue?.trim() || '' : '';
        
        // Simple name parsing
        const nameParts = fullNameText === 'Set now' ? [] : fullNameText.split(' ');
        
        if (firstNameInput) firstNameInput.value = nameParts[0] || '';
        if (middleNameInput) middleNameInput.value = nameParts.length > 2 ? nameParts.slice(1, -1).join(' ') : '';
        if (lastNameInput) lastNameInput.value = nameParts.length > 1 ? nameParts[nameParts.length - 1] : '';
        
        openModal('fullnameModal');
    }

    function openUniversalModal(config, row) {
        const titleElement = document.getElementById('modalTitle');
        const inputElement = document.getElementById('modalInput');
        
        if (titleElement) {
            titleElement.innerText = config.title;
        }
        
        // Get current value from the row
        const valueSpan = row.querySelector('.info-value');
        const currentValue = valueSpan ? valueSpan.childNodes[0]?.nodeValue?.trim() || '' : '';
        
        if (inputElement) {
            inputElement.value = currentValue === 'Set now' ? '' : currentValue;
            inputElement.placeholder = `Enter your ${config.title.toLowerCase()}...`;
            inputElement.style.borderColor = "#eee";
        }
        
        openModal('universalModal');
    }

    // Save functions
    window.saveFullName = async function () {
        const firstName = document.getElementById('firstNameInput')?.value.trim() || '';
        const middleName = document.getElementById('middleNameInput')?.value.trim() || '';
        const lastName = document.getElementById('lastNameInput')?.value.trim() || '';

        if (!firstName || !lastName) {
            showToast('First name and last name are required', true);
            return;
        }

        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        
        try {
            const response = await fetch('/AccountProfile/UpdateFullName', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify({
                    firstName: firstName,
                    middleName: middleName,
                    lastName: lastName
                })
            });

            const result = await response.json();

            if (result.success) {
                updateRowDisplay(currentRow, result.displayValue);
                showToast('Name updated successfully');
                closeModal('fullnameModal');
            } else {
                showToast(result.message || 'Error updating name', true);
            }
        } catch (error) {
            console.error('Error:', error);
            showToast('Network error', true);
        }
    };

    window.saveField = async function () {
        const input = document.getElementById('modalInput');
        if (!input) return;
        
        const value = input.value.trim();

        if (!currentField) {
            console.error('No current field set');
            return;
        }

        if (currentField === 'email' && !validateEmail(value)) {
            showToast('Please enter a valid email address', true);
            return;
        }

        if (value === "") {
            showToast('This field cannot be empty', true);
            input.style.borderColor = "#000";
            return;
        }

        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

        try {
            const response = await fetch('/AccountProfile/UpdateField', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify({
                    field: currentField,
                    value: value
                })
            });

            const result = await response.json();

            if (result.success) {
                updateRowDisplay(currentRow, result.displayValue || value);
                showToast(result.message || 'Field updated successfully');
                closeModal('universalModal');
            } else {
                showToast(result.message || 'Error updating field', true);
            }
        } catch (error) {
            console.error('Error:', error);
            showToast('Network error', true);
        }
    };

    window.savePassword = async function () {
        const currentPassword = document.getElementById('currentPassword')?.value || '';
        const newPassword = document.getElementById('newPassword')?.value || '';
        const confirmPassword = document.getElementById('confirmPassword')?.value || '';

        if (!currentPassword || !newPassword || !confirmPassword) {
            showToast('All fields are required', true);
            return;
        }

        if (newPassword.length < 6) {
            showToast('Password must be at least 6 characters', true);
            return;
        }

        if (newPassword !== confirmPassword) {
            showToast('New passwords do not match', true);
            return;
        }

        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

        try {
            const response = await fetch('/AccountProfile/UpdatePassword', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify({
                    currentPassword: currentPassword,
                    newPassword: newPassword,
                    confirmPassword: confirmPassword
                })
            });

            const result = await response.json();

            if (result.success) {
                showToast('Password updated successfully');
                closeModal('passwordModal');
                document.getElementById('currentPassword').value = '';
                document.getElementById('newPassword').value = '';
                document.getElementById('confirmPassword').value = '';
            } else {
                showToast(result.message || 'Error updating password', true);
            }
        } catch (error) {
            console.error('Error:', error);
            showToast('Network error', true);
        }
    };

    function updateRowDisplay(row, value) {
        if (!row) return;
        
        const valueSpan = row.querySelector('.info-value');
        if (!valueSpan) return;

        if (!value || value === 'Set now') {
            valueSpan.innerHTML = `Set now <i class="bi bi-chevron-right"></i>`;
            valueSpan.classList.add('text-muted');
        } else {
            valueSpan.innerHTML = `${value} <i class="bi bi-chevron-right"></i>`;
            valueSpan.classList.remove('text-muted');
        }
    }

    function validateEmail(email) {
        const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return re.test(email);
    }

    // Close on overlay click
    window.addEventListener('click', function(e) {
        if (e.target.classList.contains('modal-overlay')) {
            closeModal(e.target.id);
        }
    });
});

function openModal(id) {
    const modal = document.getElementById(id);
    if (modal) {
        modal.style.display = 'flex';
        document.body.style.overflow = 'hidden';
    }
}

function closeModal(id) {
    const modal = document.getElementById(id);
    if (modal) {
        modal.style.display = 'none';
        document.body.style.overflow = '';
        
        // Clear inputs based on modal type
        if (id === 'passwordModal') {
            document.getElementById('currentPassword').value = '';
            document.getElementById('newPassword').value = '';
            document.getElementById('confirmPassword').value = '';
        } else if (id === 'universalModal') {
            document.getElementById('modalInput').value = '';
        } else if (id === 'fullnameModal') {
            document.getElementById('firstNameInput').value = '';
            document.getElementById('middleNameInput').value = '';
            document.getElementById('lastNameInput').value = '';
        }
    }
}

function showToast(msg, isErr = false) {
    let toast = document.querySelector('.toast-container');
    if (!toast) {
        toast = document.createElement('div');
        toast.className = 'toast-container';
        document.body.appendChild(toast);
    }
    
    toast.textContent = msg;
    toast.className = 'toast-container' + (isErr ? ' error' : '');
    
    setTimeout(() => toast.classList.add('show'), 10);
    setTimeout(() => {
        toast.classList.remove('show');
    }, 3000);
}