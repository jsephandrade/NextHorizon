// Global variables
let currentViewType = 'active';

// Load consumers on page load
document.addEventListener('DOMContentLoaded', function() {
    loadConsumers('active');
    
    // Search functionality
    const searchInput = document.getElementById('searchInput');
    if (searchInput) {
        searchInput.addEventListener('keyup', function() {
            const searchTerm = this.value;
            filterConsumers(searchTerm);
        });
    }
});

// Load consumers from server
async function loadConsumers(viewType) {
    currentViewType = viewType;
    
    try {
        const response = await fetch(`/Admin/GetConsumers?viewType=${viewType}`);
        const data = await response.json();
        
        if (viewType === 'active') {
            renderActiveConsumers(data);
        } else {
            renderArchivedConsumers(data);
        }
    } catch (error) {
        console.error('Error loading consumers:', error);
        triggerToast('Error loading consumers', 'text-danger', 'bi-exclamation-triangle-fill');
    }
}

// Render active consumers table
function renderActiveConsumers(consumers) {
    const tbody = document.getElementById('activeConsumersBody');
    if (!tbody) return;
    
    if (!consumers || consumers.length === 0) {
        tbody.innerHTML = '<tr><td colspan="6" class="text-center py-4 text-muted">No active consumers found</td></tr>';
        return;
    }
    
    tbody.innerHTML = '';
    
    consumers.forEach(consumer => {
        const row = document.createElement('tr');
        row.innerHTML = `
            <td class="border-start text-secondary">${escapeHtml(consumer.fullName)}</td>
            <td class="border-start text-secondary">${escapeHtml(consumer.phoneNumber)}</td>
            <td class="border-start text-secondary">${escapeHtml(consumer.email)}</td>
            <td class="border-start text-secondary">${escapeHtml(consumer.address)}</td>
            <td class="border-start text-secondary">${escapeHtml(consumer.dateJoined)}</td>
            <td class="border-start">
                <div class="action-icons">
                    <i class="bi bi-pencil me-3 text-primary" onclick="openEditModal(${consumer.consumerId})" style="cursor: pointer;"></i>
                    <i class="bi bi-trash text-danger" onclick="openConfirmModal('delete', '${escapeHtml(consumer.fullName)}', ${consumer.consumerId})" style="cursor: pointer;"></i>
                </div>
            </td>
        `;
        tbody.appendChild(row);
    });
}

// Render archived consumers table
function renderArchivedConsumers(consumers) {
    const tbody = document.getElementById('archivedConsumersBody');
    if (!tbody) return;
    
    if (!consumers || consumers.length === 0) {
        tbody.innerHTML = '<tr><td colspan="5" class="text-center py-4 text-muted">No archived consumers found</td></tr>';
        return;
    }
    
    tbody.innerHTML = '';
    
    consumers.forEach(consumer => {
        const row = document.createElement('tr');
        row.innerHTML = `
            <td class="border-start text-secondary">${escapeHtml(consumer.fullName)}</td>
            <td class="border-start text-secondary">${escapeHtml(consumer.phoneNumber)}</td>
            <td class="border-start text-secondary">${escapeHtml(consumer.email)}</td>
            <td class="border-start text-secondary">${escapeHtml(consumer.address)}</td>
            <td class="border-start">
                <button class="btn btn-sm btn-outline-dark rounded-pill py-0 px-3"
                        onclick="openConfirmModal('restore', '${escapeHtml(consumer.fullName)}', ${consumer.consumerId})" 
                        style="font-size: 0.7rem;">
                    Restore
                </button>
            </td>
        `;
        tbody.appendChild(row);
    });
}

// Filter consumers on current view
function filterConsumers(searchTerm) {
    const viewType = currentViewType;
    const tbody = document.getElementById(viewType === 'active' ? 'activeConsumersBody' : 'archivedConsumersBody');
    if (!tbody) return;
    
    const rows = tbody.getElementsByTagName('tr');
    const searchLower = searchTerm.toLowerCase();
    
    for (let row of rows) {
        if (row.cells.length === 0) continue;
        
        const name = row.cells[0]?.textContent.toLowerCase() || '';
        const number = row.cells[1]?.textContent.toLowerCase() || '';
        const email = row.cells[2]?.textContent.toLowerCase() || '';
        const address = row.cells[3]?.textContent.toLowerCase() || '';
        
        if (name.includes(searchLower) || number.includes(searchLower) || 
            email.includes(searchLower) || address.includes(searchLower)) {
            row.style.display = '';
        } else {
            row.style.display = 'none';
        }
    }
}

// Tab Switching
function switchUserTab(viewName) {
    const activeView = document.getElementById('view-active');
    const archivedView = document.getElementById('view-archived');
    const activeTabBtn = document.getElementById('tab-active');
    const archivedTabBtn = document.getElementById('tab-archived');

    if (viewName === 'active') {
        activeView.classList.remove('d-none');
        archivedView.classList.add('d-none');
        activeTabBtn.classList.add('bg-dark', 'text-white', 'fw-bold');
        activeTabBtn.classList.remove('text-muted');
        archivedTabBtn.classList.add('text-muted');
        archivedTabBtn.classList.remove('bg-dark', 'text-white', 'fw-bold');
        loadConsumers('active');
    } else {
        archivedView.classList.remove('d-none');
        activeView.classList.add('d-none');
        archivedTabBtn.classList.add('bg-dark', 'text-white', 'fw-bold');
        archivedTabBtn.classList.remove('text-muted');
        activeTabBtn.classList.add('text-muted');
        activeTabBtn.classList.remove('bg-dark', 'text-white', 'fw-bold');
        loadConsumers('archived');
    }
    
    // Clear search input
    document.getElementById('searchInput').value = '';
}

// Toast Helper
function triggerToast(msg, iconClass = "text-success", iconType = "bi-check-circle-fill") {
    const toastMessage = document.getElementById('toastMessage');
    const toastIcon = document.getElementById('toastIcon');
    
    if (toastMessage) toastMessage.innerText = msg;
    if (toastIcon) {
        toastIcon.className = `bi ${iconType} ${iconClass} fs-5`;
    }

    const toastEl = document.getElementById('actionToast');
    if (toastEl) {
        const toast = new bootstrap.Toast(toastEl);
        toast.show();
    }
}

// Open Edit Modal (implement later)
function openEditModal(consumerId) {
    // Fetch consumer details and populate modal
    triggerToast('Edit functionality coming soon', 'text-warning', 'bi-exclamation-triangle-fill');
}

// Save Changes (implement later)
function saveChanges() {
    bootstrap.Modal.getInstance(document.getElementById('editModal')).hide();
    triggerToast("Changes saved successfully!");
}

// Open Confirm Modal
function openConfirmModal(type, name, consumerId) {
    const title = document.getElementById('confirmTitle');
    const msg = document.getElementById('confirmMessage');
    const icon = document.getElementById('confirmIcon');
    const btn = document.getElementById('confirmBtn');

    if (type === 'delete') {
        title.innerText = "Delete User?";
        msg.innerText = `${name} will be moved to archives!`;
        icon.innerHTML = '<i class="bi bi-trash text-danger" style="font-size: 3rem;"></i>';
        btn.className = "btn btn-danger rounded-pill px-4";
        btn.onclick = () => {
            deleteConsumer(consumerId, name);
        };
    } else {
        title.innerText = "Restore User?";
        msg.innerText = `Restore ${name} to active list?`;
        icon.innerHTML = '<i class="bi bi-arrow-counterclockwise text-success" style="font-size: 3rem;"></i>';
        btn.className = "btn btn-success rounded-pill px-4";
        btn.onclick = () => {
            restoreConsumer(consumerId, name);
        };
    }
    new bootstrap.Modal(document.getElementById('confirmModal')).show();
}

// Delete consumer function
async function deleteConsumer(consumerId, name) {
    try {
        const response = await fetch('/Admin/DeleteConsumer', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ consumerId: consumerId })
        });
        
        const data = await response.json();
        
        if (data.success) {
            bootstrap.Modal.getInstance(document.getElementById('confirmModal')).hide();
            triggerToast(`${name} moved to archive!`, "text-danger", "bi-archive-fill");
            loadConsumers(currentViewType);
        } else {
            triggerToast(data.message, "text-danger", "bi-exclamation-triangle-fill");
        }
    } catch (error) {
        triggerToast('Error deleting consumer', "text-danger", "bi-exclamation-triangle-fill");
    }
}

// Restore consumer function
async function restoreConsumer(consumerId, name) {
    try {
        const response = await fetch('/Admin/RestoreConsumer', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ consumerId: consumerId })
        });
        
        const data = await response.json();
        
        if (data.success) {
            bootstrap.Modal.getInstance(document.getElementById('confirmModal')).hide();
            triggerToast(`${name} restored!`, "text-success", "bi-check-circle-fill");
            loadConsumers(currentViewType);
        } else {
            triggerToast(data.message, "text-danger", "bi-exclamation-triangle-fill");
        }
    } catch (error) {
        triggerToast('Error restoring consumer', "text-danger", "bi-exclamation-triangle-fill");
    }
}

// Helper function to escape HTML
function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}