/**
 * SETTINGS.JS - Admin Portal
 * Handles Global Branding, Security, Moderation, and Audit Logging
 */

let pendingAction = null;
let currentAuditPage = 1;
let currentAdminPage = 1;
let currentRevokedPage = 1;
let currentEditMode = false;
let currentStaffId = null;

document.addEventListener('DOMContentLoaded', () => {
    // Load data
    loadActiveAdmins();
    loadRevokedAdmins();
    loadAuditLogs();

    // Default tab logic
    const defaultTabEl = document.querySelector('#v-pills-tab button[data-bs-target="#tab-account"]');
    const defaultPane = document.getElementById('tab-account');

    if (defaultTabEl && defaultPane) {
        defaultTabEl.classList.add('active');
        defaultPane.classList.add('show', 'active');
        if (typeof bootstrap !== 'undefined') {
            const tabTrigger = bootstrap.Tab.getOrCreateInstance(defaultTabEl);
            tabTrigger.show();
        }
    }

    // Modal confirmation logic
    const confirmBtn = document.getElementById('confirmBtnExecute');
    if (confirmBtn) {
        const newConfirmBtn = confirmBtn.cloneNode(true);
        confirmBtn.parentNode.replaceChild(newConfirmBtn, confirmBtn);

        newConfirmBtn.addEventListener('click', () => {
            if (typeof pendingAction === 'function') {
                pendingAction();
                pendingAction = null;
            }
        });
    }

    // Search filter
    const searchInput = document.getElementById('adminSearchInput');
    if (searchInput) {
        searchInput.addEventListener('keyup', filterAdminTable);
    }

    // Audit log search
    const auditSearch = document.getElementById('auditSearchInput');
    if (auditSearch) {
        auditSearch.addEventListener('keyup', () => {
            currentAuditPage = 1;
            loadAuditLogs();
        });
    }

    // Hash-based tab navigation
    var hash = window.location.hash;
    if (hash) {
        var tabTriggerEl = document.querySelector(`[data-bs-target="${hash}"]`);
        if (tabTriggerEl) {
            var tab = new bootstrap.Tab(tabTriggerEl);
            tab.show();
        }
    }
});

/**
 * Load Active Admins
 */
async function loadActiveAdmins() {
    try {
        const response = await fetch('/Admin/GetActiveAdmins');
        const data = await response.json();
        
        const tbody = document.getElementById('adminTableBody');
        if (!tbody) return;
        
        // Check if data has error
        if (data.error) {
            tbody.innerHTML = `<tr><td colspan="6" class="text-center py-4 text-danger">Error: ${data.error}</td></tr>`;
            return;
        }
        
        // Check if data is an array
        if (!Array.isArray(data)) {
            console.error('Expected array but got:', data);
            tbody.innerHTML = '<tr><td colspan="6" class="text-center py-4 text-muted">No active admins found</td></tr>';
            return;
        }
        
        if (data.length === 0) {
            tbody.innerHTML = '<tr><td colspan="6" class="text-center py-4 text-muted">No active admins found</td></tr>';
            return;
        }
        
        tbody.innerHTML = '';
        data.forEach(admin => {
            const row = document.createElement('tr');
            row.className = 'admin-row';
            row.id = `adminRow_${admin.staffId}`;
            row.innerHTML = `
                <td class="ps-3">
                    <div class="fw-bold name-cell">${escapeHtml(admin.fullName)}</div>
                    <div class="x-small text-muted email-cell">${escapeHtml(admin.email)}</div>
                </td>
                <td><span class="badge bg-light text-dark border rounded-pill fw-normal">${escapeHtml(admin.userType)}</span></td>
                <td class="x-small text-muted">${escapeHtml(admin.phone || '—')}</td>
                <td class="x-small text-muted">${formatDate(admin.createdAt)}</td>
                <td class="x-small text-muted">${admin.lastActive ? formatDate(admin.lastActive) : 'Never'}</td>
                <td class="text-end pe-3">
                    <button class="btn btn-sm btn-outline-dark border-0" onclick="editAdmin(${admin.staffId}, '${escapeHtml(admin.firstName)}', '${escapeHtml(admin.lastName)}', '${escapeHtml(admin.middleName || '')}', '${escapeHtml(admin.email)}', '${escapeHtml(admin.phone || '')}', '${escapeHtml(admin.userType)}')">
                        <i class="bi bi-pencil"></i>
                    </button>
                    <button class="btn btn-sm btn-outline-danger border-0" onclick="openRevokeModal(${admin.staffId}, '${escapeHtml(admin.fullName)}')">
                        <i class="bi bi-trash"></i>
                    </button>
                </td>
            `;
            tbody.appendChild(row);
        });
    } catch (error) {
        console.error('Error loading active admins:', error);
        const tbody = document.getElementById('adminTableBody');
        if (tbody) {
            tbody.innerHTML = '<tr><td colspan="6" class="text-center py-4 text-danger">Error loading active admins</td></tr>';
        }
    }
}

/**
 * Load Revoked Admins
 */
async function loadRevokedAdmins() {
    try {
        const response = await fetch('/Admin/GetRevokedAdmins');
        const data = await response.json();
        
        const tbody = document.getElementById('revokedTableBody');
        if (!tbody) return;
        
        // Check if data has error
        if (data.error) {
            tbody.innerHTML = `<tr><td colspan="5" class="text-center py-4 text-danger">Error: ${data.error}</td></tr>`;
            return;
        }
        
        // Check if data is an array
        if (!Array.isArray(data)) {
            console.error('Expected array but got:', data);
            tbody.innerHTML = '<tr><td colspan="5" class="text-center py-4 text-muted">No revoked admins found</td></tr>';
            return;
        }
        
        if (data.length === 0) {
            tbody.innerHTML = '<tr><td colspan="5" class="text-center py-4 text-muted">No revoked admins found</td></tr>';
            return;
        }
        
        tbody.innerHTML = '';
        data.forEach(admin => {
            const row = document.createElement('tr');
            row.id = `revokedRow_${admin.staffId}`;
            row.innerHTML = `
                <td class="ps-3">
                    <div class="fw-bold text-muted">${escapeHtml(admin.fullName)}</div>
                    <div class="x-small">${escapeHtml(admin.email)}</div>
                </td>
                <td><span class="badge bg-light text-dark border rounded-pill fw-normal">${escapeHtml(admin.userType)}</span></td>
                <td class="text-muted">${formatDate(admin.revokedAt)}</td>
                <td class="text-muted">${escapeHtml(admin.revokedBy || 'System')}</td>
                <td class="text-end pe-3">
                    <button class="btn btn-sm btn-dark rounded-pill px-3" onclick="openReinstateModal(${admin.staffId}, '${escapeHtml(admin.fullName)}')">
                        <i class="bi bi-arrow-counterclockwise me-1"></i> Reinstate
                    </button>
                </td>
            `;
            tbody.appendChild(row);
        });
    } catch (error) {
        console.error('Error loading revoked admins:', error);
        const tbody = document.getElementById('revokedTableBody');
        if (tbody) {
            tbody.innerHTML = '<tr><td colspan="5" class="text-center py-4 text-danger">Error loading revoked admins</td></tr>';
        }
    }
}   

/**
 * Load Audit Logs
 */
async function loadAuditLogs(page = 1) {
    currentAuditPage = page;
    const searchTerm = document.getElementById('auditSearchInput')?.value || '';
    
    try {
        const response = await fetch(`/Admin/GetAuditLogs?page=${page}&pageSize=50&search=${encodeURIComponent(searchTerm)}`);
        const data = await response.json();
        
        const tbody = document.getElementById('auditLogBody');
        if (!tbody) return;
        
        if (data.success && data.logs.length === 0) {
            tbody.innerHTML = '<tr><td colspan="5" class="text-center py-4 text-muted">No audit logs found</td></tr>';
            return;
        }
        
        if (data.success) {
            tbody.innerHTML = '';
            data.logs.forEach(log => {
                const statusClass = log.status === 'Success' ? 'bg-success-subtle text-success' : 'bg-danger-subtle text-danger';
                const row = document.createElement('tr');
                row.innerHTML = `
                    <td class="small">${formatDateTime(log.timestamp)}</td>
                    <td><strong>${escapeHtml(log.adminName)}</strong></td>
                    <td>${escapeHtml(log.action)}</td>
                    <td>${escapeHtml(log.target)}</td>
                    <td><span class="badge ${statusClass} rounded-pill">${escapeHtml(log.status)}</span></td>
                `;
                tbody.appendChild(row);
            });
            
            // Update pagination
            const totalPages = Math.ceil(data.totalCount / 50);
            document.getElementById('auditPaginationInfo').innerText = `Showing ${data.logs.length} of ${data.totalCount} entries`;
            document.getElementById('prevAuditBtn').disabled = page <= 1;
            document.getElementById('nextAuditBtn').disabled = page >= totalPages;
        } else {
            tbody.innerHTML = `<tr><td colspan="5" class="text-center py-4 text-danger">Error: ${data.error}</td></tr>`;
        }
    } catch (error) {
        console.error('Error loading audit logs:', error);
        showToast('Error loading audit logs', true);
    }
}

/**
 * Add/Edit Admin Functions
 */
function openInviteModal() {
    currentEditMode = false;
    currentStaffId = null;
    document.getElementById('modModalLabel').innerText = "Add New User";
    document.getElementById('modFirstName').value = '';
    document.getElementById('modLastName').value = '';
    document.getElementById('modMiddleName').value = '';
    document.getElementById('modEmail').value = '';
    document.getElementById('modPhone').value = '';
    updateRoleUI('Select Role', '');
    resetCredentialOverride();
    bootstrap.Modal.getOrCreateInstance(document.getElementById('inviteModModal')).show();
}

function editAdmin(staffId, firstName, lastName, middleName, email, phone, userType) {
    currentEditMode = true;
    currentStaffId = staffId;
    document.getElementById('modModalLabel').innerText = "Edit User";
    document.getElementById('editStaffId').value = staffId;
    document.getElementById('modFirstName').value = firstName;
    document.getElementById('modLastName').value = lastName;
    document.getElementById('modMiddleName').value = middleName;
    document.getElementById('modEmail').value = email;
    document.getElementById('modPhone').value = phone;
    updateRoleUI(userType, userType);
    resetCredentialOverride();
    bootstrap.Modal.getOrCreateInstance(document.getElementById('inviteModModal')).show();
}

function updateRoleUI(roleName, roleValue) {
    document.getElementById('selectedRoleText').innerText = roleName;
    document.getElementById('accessLevelInput').value = roleValue;
}

function toggleCredentialOverride() {
    const section = document.getElementById('credentialOverrideSection');
    const btn = document.getElementById('overrideToggleBtn');
    const isEditMode = currentEditMode;
    
    if (section.classList.contains('d-none')) {
        section.classList.remove('d-none');
        btn.innerHTML = '<i class="bi bi-x-circle me-1"></i> Cancel Credential Update';
        btn.classList.replace('btn-outline-dark', 'btn-outline-danger');
    } else {
        resetCredentialOverride();
    }
}

function resetCredentialOverride() {
    const section = document.getElementById('credentialOverrideSection');
    const btn = document.getElementById('overrideToggleBtn');
    const isEditMode = currentEditMode;
    
    section.classList.add('d-none');
    const label = isEditMode ? "Update User's Login Credentials" : "Manually assign login credentials";
    btn.innerHTML = `<i class="bi bi-shield-lock me-1"></i> ${label}`;
    btn.classList.replace('btn-outline-danger', 'btn-outline-dark');
    document.getElementById('overrideUsername').value = '';
    document.getElementById('overridePassword').value = '';
}

function processModerator() {
    const firstName = document.getElementById('modFirstName').value;
    const lastName = document.getElementById('modLastName').value;
    const middleName = document.getElementById('modMiddleName').value;
    const email = document.getElementById('modEmail').value;
    const phone = document.getElementById('modPhone').value;
    const userType = document.getElementById('accessLevelInput').value;
    const isOverriding = !document.getElementById('credentialOverrideSection').classList.contains('d-none');
    const overrideUsername = isOverriding ? document.getElementById('overrideUsername').value : '';
    const overridePassword = isOverriding ? document.getElementById('overridePassword').value : '';
    
    if (!firstName || !lastName || !email || !userType) {
        showToast('Error: Name, Email, and Role are required.', true);
        return;
    }
    
    const fullName = `${firstName} ${lastName}`;
    let confirmationMsg = `${currentEditMode ? 'Update' : 'Add'} ${userType} permissions for ${fullName}?`;
    if (isOverriding && overrideUsername) confirmationMsg += `\nUsername: ${overrideUsername}`;
    if (isOverriding && overridePassword) confirmationMsg += `\nPassword: ${overridePassword}`;
    
    openConfirmModal(
        currentEditMode ? "Confirm Update" : "Confirm Add",
        confirmationMsg,
        async () => {
            const url = currentEditMode ? '/Admin/UpdateAdmin' : '/Admin/AddAdmin';
            const body = currentEditMode ? {
                staffId: currentStaffId,
                firstName: firstName,
                lastName: lastName,
                middleName: middleName,
                email: email,
                phone: phone,
                userType: userType,
                overrideCredentials: isOverriding,
                overrideUsername: overrideUsername,
                overridePassword: overridePassword
            } : {
                firstName: firstName,
                lastName: lastName,
                middleName: middleName,
                email: email,
                phone: phone,
                userType: userType,
                overrideCredentials: isOverriding,
                overrideUsername: overrideUsername,
                overridePassword: overridePassword
            };
            
            try {
                const response = await fetch(url, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(body)
                });
                
                const data = await response.json();
                
                if (data.success) {
                    hideModal();
                    hideModal('inviteModModal');
                    showToast(data.message);
                    loadActiveAdmins();
                    loadRevokedAdmins();
                    
                    if (data.username && data.isPasswordAutoGenerated) {
                        showToast(`Username: ${data.username}, Password: ${data.password}`, false, true);
                    }
                } else {
                    showToast(data.message, true);
                }
            } catch (error) {
                showToast('Error processing request', true);
            }
        }
    );
}

/**
 * Revoke/Reinstate Functions
 */
function openRevokeModal(staffId, name) {
    const modal = document.getElementById('confirmActionModal');
    const reasonContainer = document.getElementById('confirmReasonContainer');
    reasonContainer.classList.remove('d-none');
    document.getElementById('confirmReason').value = '';
    
    openConfirmModal(
        "Revoke Access",
        `Move ${name} to revoked status? They will lose all portal access immediately.`,
        async () => {
            const reason = document.getElementById('confirmReason').value;
            if (!reason) {
                showToast('Please provide a reason for revocation', true);
                return;
            }
            
            try {
                const response = await fetch('/Admin/RevokeAdmin', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ staffId: staffId, reason: reason })
                });
                
                const data = await response.json();
                
                if (data.success) {
                    hideModal();
                    showToast(data.message);
                    loadActiveAdmins();
                    loadRevokedAdmins();
                } else {
                    showToast(data.message, true);
                }
            } catch (error) {
                showToast('Error revoking admin', true);
            }
            document.getElementById('confirmReasonContainer').classList.add('d-none');
        }
    );
}

function openReinstateModal(staffId, name) {
    const modal = document.getElementById('confirmActionModal');
    const reasonContainer = document.getElementById('confirmReasonContainer');
    reasonContainer.classList.remove('d-none');
    document.getElementById('confirmReason').value = '';
    
    openConfirmModal(
        "Reinstate Access",
        `Restore administrative permissions for ${name}?`,
        async () => {
            const reason = document.getElementById('confirmReason').value;
            
            try {
                const response = await fetch('/Admin/ReinstateAdmin', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ staffId: staffId, reason: reason })
                });
                
                const data = await response.json();
                
                if (data.success) {
                    hideModal();
                    showToast(data.message);
                    loadActiveAdmins();
                    loadRevokedAdmins();
                } else {
                    showToast(data.message, true);
                }
            } catch (error) {
                showToast('Error reinstating admin', true);
            }
            document.getElementById('confirmReasonContainer').classList.add('d-none');
        }
    );
}

/**
 * Profile Functions
 */
async function saveProfileInfo() {
    const fullName = document.getElementById('adminFullName').value;
    const email = document.getElementById('adminEmail').value;
    
    if (!fullName || !email) {
        showToast('Error: Name and Email cannot be empty.', true);
        return;
    }
    
    openConfirmModal(
        "Update Profile",
        `Change account details to ${fullName}?`,
        async () => {
            try {
                const response = await fetch('/Admin/UpdateProfile', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ fullName: fullName, email: email })
                });
                
                const data = await response.json();
                
                if (data.success) {
                    hideModal();
                    showToast(data.message);
                } else {
                    showToast(data.message, true);
                }
            } catch (error) {
                showToast('Error updating profile', true);
            }
        }
    );
}

async function requestPasswordChange() {
    const currentPass = document.getElementById('currentPass').value;
    const newPass = document.getElementById('newPass').value;
    const confirmPass = document.getElementById('confirmNewPass').value;
    
    if (!currentPass) {
        showToast('Error: Current password is required.', true);
        return;
    }
    if (newPass.length < 6) {
        showToast('Error: New password must be at least 6 characters.', true);
        return;
    }
    if (newPass !== confirmPass) {
        showToast('Error: New passwords do not match.', true);
        return;
    }
    
    openConfirmModal(
        "Confirm Password Change",
        "Update your security credentials? You will need the new password for your next login.",
        async () => {
            try {
                const response = await fetch('/Admin/ChangePassword', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ 
                        currentPassword: currentPass,
                        newPassword: newPass,
                        confirmPassword: confirmPass
                    })
                });
                
                const data = await response.json();
                
                if (data.success) {
                    hideModal();
                    showToast(data.message);
                    document.getElementById('currentPass').value = "";
                    document.getElementById('newPass').value = "";
                    document.getElementById('confirmNewPass').value = "";
                } else {
                    showToast(data.message, true);
                }
            } catch (error) {
                showToast('Error changing password', true);
            }
        }
    );
}

/**
 * UTILITIES
 */
function openConfirmModal(title, message, action, iconClass = 'bi-exclamation-circle', iconColor = 'text-warning') {
    const modalEl = document.getElementById('confirmActionModal');
    const iconEl = document.getElementById('confirmIcon');
    if (!modalEl) return;
    
    document.getElementById('confirmTitle').innerText = title;
    document.getElementById('confirmBody').innerText = message;
    
    if (iconEl) {
        iconEl.className = `bi ${iconClass} ${iconColor} display-4 mb-3`;
    }
    
    pendingAction = action;
    const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
    modal.show();
}

function hideModal(modalId = 'confirmActionModal') {
    const modalElement = document.getElementById(modalId);
    if (!modalElement) return;
    const modal = bootstrap.Modal.getInstance(modalElement);
    if (modal) modal.hide();
}

function showToast(message, isError = false, isInfo = false) {
    const toastEl = document.getElementById('settingsToast');
    const toastMsg = document.getElementById('toastMsg');
    const toastIcon = toastEl.querySelector('.toast-body i');
    
    if (toastEl && toastMsg) {
        toastMsg.innerText = message;
        if (isError) {
            toastIcon.className = 'bi bi-exclamation-triangle-fill me-2 text-danger';
        } else if (isInfo) {
            toastIcon.className = 'bi bi-info-circle-fill me-2 text-info';
        } else {
            toastIcon.className = 'bi bi-check-circle-fill me-2 text-success';
        }
        const toast = bootstrap.Toast.getOrCreateInstance(toastEl);
        toast.show();
    }
}

function filterAdminTable() {
    const input = document.getElementById('adminSearchInput');
    const filter = input.value.toLowerCase();
    const rows = document.querySelectorAll('.admin-row');
    
    rows.forEach(row => {
        const text = row.innerText.toLowerCase();
        row.style.display = text.includes(filter) ? "" : "none";
    });
}

function formatDate(dateString) {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-PH', { month: 'short', day: 'numeric', year: 'numeric' });
}

function formatDateTime(dateString) {
    const date = new Date(dateString);
    return date.toLocaleString('en-PH', { month: 'short', day: 'numeric', year: 'numeric', hour: '2-digit', minute: '2-digit' });
}

function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}