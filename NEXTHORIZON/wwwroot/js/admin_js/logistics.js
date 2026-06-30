/**
 * Logistics Partner Module - Admin Logic
 * Handles CRUD operations, filtering, search, and performance metrics
 */

let currentFilter = 'active';
let currentEditId = null;

// Initialize on page load
document.addEventListener('DOMContentLoaded', function () {
    loadLogisticsStats();
    loadLogistics(currentFilter);
    
    // Set up search
    const searchInput = document.getElementById('logisticsSearch');
    if (searchInput) {
        let searchTimeout;
        searchInput.addEventListener('keyup', function () {
            clearTimeout(searchTimeout);
            searchTimeout = setTimeout(() => {
                loadLogistics(currentFilter, this.value);
            }, 500);
        });
    }
});

// Load logistics statistics for dashboard
async function loadLogisticsStats() {
    try {
        const response = await fetch('/Admin/GetLogisticsStats');
        const stats = await response.json();
        
        if (!stats.error) {
            document.getElementById('totalCount').innerText = stats.totalCount || 0;
            document.getElementById('activeCount').innerText = stats.activeCount || 0;
            document.getElementById('inactiveCount').innerText = stats.inactiveCount || 0;
            document.getElementById('archivedCount').innerText = stats.archivedCount || 0;
        }
    } catch (error) {
        console.error('Error loading stats:', error);
    }
}

// Load logistics partners from API
async function loadLogistics(statusFilter = 'active', searchTerm = '') {
    try {
        let url = '/Admin/GetLogistics';
        const params = new URLSearchParams();
        
        // Format status filter properly
        let formattedStatus = statusFilter;
        if (statusFilter === 'active') formattedStatus = 'Active';
        else if (statusFilter === 'inactive') formattedStatus = 'Inactive';
        else if (statusFilter === 'archived') formattedStatus = 'Archived';
        
        // Only add statusFilter if it's not 'total' and is a valid status
        if (formattedStatus && formattedStatus !== 'total' && 
            ['Active', 'Inactive', 'Archived'].includes(formattedStatus)) {
            params.append('statusFilter', formattedStatus);
        }
        
        if (searchTerm) {
            params.append('searchTerm', searchTerm);
        }
        
        if (params.toString()) {
            url += '?' + params.toString();
        }
        
        const response = await fetch(url);
        const result = await response.json();
        
        if (result.error) {
            console.error('Error loading logistics:', result.error);
            return;
        }
        
        const logistics = Array.isArray(result) ? result : [];
        
        // Separate into sections
        const activePartners = logistics.filter(l => l.status === 'Active');
        const inactivePartners = logistics.filter(l => l.status === 'Inactive');
        const archivedPartners = logistics.filter(l => l.status === 'Archived');
        
        // Render sections
        renderActivePartners(activePartners);
        renderInactivePartners(inactivePartners);
        renderArchivedPartners(archivedPartners);
        
        // Update badges
        document.getElementById('activeBadge').innerText = activePartners.length;
        document.getElementById('inactiveBadge').innerText = inactivePartners.length;
        document.getElementById('archivedBadge').innerText = archivedPartners.length;
        
    } catch (error) {
        console.error('Error loading logistics:', error);
        showToast('Error loading logistics partners', 'error');
    }
}


// Render active partners
function renderActivePartners(partners) {
    const container = document.getElementById('activeContainer');
    if (!container) return;
    
    if (partners.length === 0) {
        container.innerHTML = `
            <div class="col-12 text-center py-5">
                <i class="bi bi-truck fs-1 text-muted"></i>
                <p class="text-muted mt-2">No active logistics partners</p>
                <button class="btn btn-sm btn-dark rounded-pill mt-2" onclick="openAddPartnerModal()">
                    <i class="bi bi-plus-lg me-1"></i>Add Partner
                </button>
            </div>
        `;
        return;
    }
    
    container.innerHTML = partners.map(partner => `
        <div class="col-sm-6 col-lg-4 col-xl-3 partner-card" data-id="${partner.logisticsId}">
            <div class="card border-0 shadow-sm rounded-4 h-100 transition-hover">
                <div class="card-body p-4">
                    <div class="d-flex justify-content-between align-items-start mb-3">
                        <div class="bg-light rounded-3 p-2 d-flex align-items-center justify-content-center border" style="width: 60px; height: 60px;">
                            ${partner.logoBase64 ? 
                                `<img src="${partner.logoBase64}" style="width: 50px; height: 50px; object-fit: cover;" onerror="this.src='/images/default-logo.png'">` : 
                                `<i class="bi bi-truck fs-1 text-dark"></i>`
                            }
                        </div>
                        <div class="form-check form-switch">
                            <input class="form-check-input custom-switch" type="checkbox" role="switch" 
                                   id="switch-${partner.logisticsId}" checked
                                   onchange="handleStatusToggle('${escapeHtml(partner.courierName)}', ${partner.logisticsId}, this, false)">
                        </div>
                    </div>
                    <h5 class="fw-bold mb-1 courier-name">${escapeHtml(partner.courierName)}</h5>
                    <span class="badge ${partner.serviceType === 'Express Delivery' ? 'bg-warning-subtle text-warning' : 'bg-info-subtle text-info'} border rounded-pill px-3 mb-3">${escapeHtml(partner.serviceType)}</span>
                    
                    <!-- Performance Metrics (Auto-calculated from orders) -->
                    <div class="mb-3">
                        <div class="d-flex justify-content-between mb-1">
                            <span class="x-small text-muted">Success Rate</span>
                            <span class="x-small fw-bold">${partner.successRate}%</span>
                        </div>
                        <div class="progress" style="height: 4px;">
                            <div class="progress-bar bg-success" role="progressbar" style="width: ${partner.successRate}%;"></div>
                        </div>
                        <div class="d-flex justify-content-between mt-2">
                            <span class="x-small text-muted">Avg Delivery</span>
                            <span class="x-small fw-bold">${partner.avgDeliveryDays} days</span>
                        </div>
                        ${partner.last30DaysOrders ? `
                        <div class="d-flex justify-content-between mt-1">
                            <span class="x-small text-muted">Orders (30d)</span>
                            <span class="x-small fw-bold">${partner.last30DaysOrders}</span>
                        </div>
                        ` : ''}
                    </div>
                    
                    <div class="d-flex gap-2">
                        <button class="btn btn-outline-dark btn-sm w-100 rounded-pill fw-bold" 
                                onclick="viewPerformance(${partner.logisticsId}, '${escapeHtml(partner.courierName)}')">
                            <i class="bi bi-graph-up me-1"></i>Performance
                        </button>
                        <button class="btn btn-outline-danger btn-sm rounded-pill px-3" 
                                onclick="confirmDeletePartner(${partner.logisticsId}, '${escapeHtml(partner.courierName)}')">
                            <i class="bi bi-trash3"></i>
                        </button>
                    </div>
                </div>
            </div>
        </div>
    `).join('');
}

// Render inactive partners
function renderInactivePartners(partners) {
    const container = document.getElementById('inactiveContainer');
    if (!container) return;
    
    if (partners.length === 0) {
        container.innerHTML = '<div class="col-12 text-center py-5"><p class="text-muted">No inactive logistics partners</p></div>';
        return;
    }
    
    container.innerHTML = partners.map(partner => `
        <div class="col-sm-6 col-lg-4 col-xl-3 partner-card" data-id="${partner.logisticsId}">
            <div class="card border-0 shadow-sm rounded-4 h-100 opacity-75">
                <div class="card-body p-4 text-center">
                    <div class="bg-light rounded-3 mx-auto mb-3 d-flex align-items-center justify-content-center" style="width: 60px; height: 60px;">
                        ${partner.logoBase64 ? 
                            `<img src="${partner.logoBase64}" style="width: 50px; height: 50px; object-fit: cover;">` : 
                            `<i class="bi bi-truck fs-1 text-muted"></i>`
                        }
                    </div>
                    <h6 class="fw-bold mb-1">${escapeHtml(partner.courierName)}</h6>
                    <span class="badge bg-secondary rounded-pill px-3 mb-2">${partner.serviceType}</span>
                    <p class="x-small text-muted mb-3">${partner.status === 'Inactive' ? 'Disabled for maintenance' : 'Temporarily unavailable'}</p>
                    <div class="d-flex gap-2">
                        <button class="btn btn-sm btn-dark w-100 rounded-pill" onclick="handleStatusToggle('${escapeHtml(partner.courierName)}', ${partner.logisticsId}, null, true)">
                            <i class="bi bi-play-fill me-1"></i>Enable Service
                        </button>
                        <button class="btn btn-sm btn-outline-danger rounded-pill px-3" onclick="confirmDeletePartner(${partner.logisticsId}, '${escapeHtml(partner.courierName)}')">
                            <i class="bi bi-archive"></i>
                        </button>
                    </div>
                </div>
            </div>
        </div>
    `).join('');
}

// Render archived partners
function renderArchivedPartners(partners) {
    const container = document.getElementById('archiveContainer');
    if (!container) return;
    
    if (partners.length === 0) {
        container.innerHTML = '<div class="col-12 text-center py-5"><p class="text-muted">No archived logistics partners</p></div>';
        return;
    }
    
    container.innerHTML = partners.map(partner => `
        <div class="col-sm-6 col-lg-4 col-xl-3 partner-card" data-id="${partner.logisticsId}">
            <div class="card border-0 shadow-sm rounded-4 h-100 bg-white">
                <div class="card-body p-4 text-center">
                    <div class="bg-light rounded-circle mx-auto mb-3 d-flex align-items-center justify-content-center" style="width: 50px; height: 50px;">
                        <i class="bi bi-archive text-muted fs-4"></i>
                    </div>
                    <h6 class="fw-bold mb-1">${escapeHtml(partner.courierName)}</h6>
                    <p class="x-small text-muted mb-2">Archived</p>
                    <button class="btn btn-sm btn-outline-dark w-100 rounded-pill" onclick="restorePartner(${partner.logisticsId}, '${escapeHtml(partner.courierName)}')">
                        <i class="bi bi-arrow-counterclockwise me-1"></i>Restore
                    </button>
                </div>
            </div>
        </div>
    `).join('');
}

// Filter partners by status
function filterPartners(filter) {
    currentFilter = filter;
    
    const activeSection = document.getElementById('activeSection');
    const inactiveSection = document.getElementById('inactiveSection');
    const archiveSection = document.getElementById('archiveSection');
    const filterCards = document.querySelectorAll('.filter-card');
    
    filterCards.forEach(card => card.classList.remove('active-filter'));
    const activeCard = document.getElementById(`stat-${filter}`);
    if (activeCard) activeCard.classList.add('active-filter');
    
    // Show/hide sections
    if (activeSection) {
        activeSection.style.display = (filter === 'total' || filter === 'active') ? 'block' : 'none';
    }
    if (inactiveSection) {
        inactiveSection.style.display = (filter === 'total' || filter === 'inactive') ? 'block' : 'none';
    }
    if (archiveSection) {
        archiveSection.style.display = (filter === 'total' || filter === 'archived') ? 'block' : 'none';
    }
    
    // Load data with filter - keep the original filter value for UI, but let loadLogistics handle formatting
    const searchTerm = document.getElementById('logisticsSearch')?.value || '';
    loadLogistics(filter, searchTerm);
}

// Open add/edit modal
function openAddPartnerModal(partner = null) {
    const modalElement = document.getElementById('addPartnerModal');
    if (!modalElement) return;
    
    const modal = new bootstrap.Modal(modalElement);
    const modalTitle = document.getElementById('modalTitle');
    
    if (partner) {
        currentEditId = partner.logisticsId;
        modalTitle.innerText = 'Edit Logistics Partner';
        
        // Populate form - ONLY fields that exist in the modal
        document.getElementById('courierName').value = partner.courierName || '';
        document.getElementById('serviceType').value = partner.serviceType || 'Standard Delivery';
        document.getElementById('contactPerson').value = partner.contactPerson || '';
        document.getElementById('contactEmail').value = partner.contactEmail || '';
        document.getElementById('contactPhone').value = partner.contactPhone || '';
        document.getElementById('trackingUrl').value = partner.trackingUrlTemplate || '';
        document.getElementById('isPreferred').checked = partner.isPreferred || false;
        document.getElementById('isActive').checked = partner.status === 'Active';
        
        if (partner.logoBase64) {
            document.getElementById('logoPreview').src = partner.logoBase64;
        }
    } else {
        currentEditId = null;
        modalTitle.innerText = 'Add Logistics Partner';
        // Reset form
        document.getElementById('logisticsForm').reset();
        document.getElementById('logoPreview').src = '/images/default-logo.png';
        document.getElementById('isActive').checked = true;
        document.getElementById('isPreferred').checked = false;
    }
    
    modal.show();
}

// Save logistics partner (UPDATED - removed performance fields)
async function saveLogisticsPartner() {
    // Get form values - ONLY fields that exist in the modal
    const courierNameInput = document.getElementById('courierName');
    const serviceTypeSelect = document.getElementById('serviceType');
    const contactPersonInput = document.getElementById('contactPerson');
    const contactEmailInput = document.getElementById('contactEmail');
    const contactPhoneInput = document.getElementById('contactPhone');
    const trackingUrlInput = document.getElementById('trackingUrl');
    const isPreferredCheckbox = document.getElementById('isPreferred');
    const isActiveCheckbox = document.getElementById('isActive');
    const logoPreview = document.getElementById('logoPreview');
    const logoUpload = document.getElementById('logoUpload');
    
    // Validate required fields
    if (!courierNameInput || !courierNameInput.value.trim()) {
        showToast('Please enter courier name', 'error');
        return;
    }
    
    const name = courierNameInput.value.trim();
    const serviceType = serviceTypeSelect ? serviceTypeSelect.value : 'Standard Delivery';
    
    // Handle logo
    let logoBase64 = null;
    let logoFilename = null;
    let logoContentType = null;
    
    if (logoUpload && logoUpload.files && logoUpload.files[0]) {
        logoFilename = logoUpload.files[0].name;
        logoContentType = logoUpload.files[0].type;
        logoBase64 = logoPreview ? logoPreview.src : null;
    } else if (logoPreview && logoPreview.src && !logoPreview.src.includes('default-logo.png')) {
        logoBase64 = logoPreview.src;
    }
    
    const requestData = {
        logisticsId: currentEditId,
        courierName: name,
        serviceType: serviceType,
        logoBase64: logoBase64,
        logoFilename: logoFilename,
        logoContentType: logoContentType,
        status: isActiveCheckbox && isActiveCheckbox.checked ? 'Active' : 'Inactive',
        contactPerson: contactPersonInput ? contactPersonInput.value || null : null,
        contactEmail: contactEmailInput ? contactEmailInput.value || null : null,
        contactPhone: contactPhoneInput ? contactPhoneInput.value || null : null,
        trackingUrlTemplate: trackingUrlInput ? trackingUrlInput.value || null : null,
        isPreferred: isPreferredCheckbox ? isPreferredCheckbox.checked : false,
        sortOrder: 0
    };
    
    try {
        showToast('Saving...', 'info');
        
        const response = await fetch('/Admin/SaveLogistics', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(requestData)
        });
        
        const result = await response.json();
        
        if (result.success) {
            showToast(result.message, 'success');
            const modal = bootstrap.Modal.getInstance(document.getElementById('addPartnerModal'));
            if (modal) modal.hide();
            loadLogisticsStats();
            loadLogistics(currentFilter);
        } else {
            showToast(result.message, 'error');
        }
    } catch (error) {
        console.error('Error saving partner:', error);
        showToast('Failed to save logistics partner', 'error');
    }
}

// View detailed performance
async function viewPerformance(logisticsId, name) {
    const modalElement = document.getElementById('performanceModal');
    if (!modalElement) return;
    
    const modal = new bootstrap.Modal(modalElement);
    const perfName = document.getElementById('perfName');
    const perfContent = document.getElementById('performanceContent');
    
    if (perfName) perfName.innerHTML = `${escapeHtml(name)} <small class="text-muted fs-6">Performance Report</small>`;
    if (perfContent) {
        perfContent.innerHTML = `
            <div class="text-center py-4">
                <div class="spinner-border text-dark" role="status">
                    <span class="visually-hidden">Loading...</span>
                </div>
            </div>
        `;
    }
    modal.show();
    
    try {
        const response = await fetch(`/Admin/GetLogisticsPerformance?logisticsId=${logisticsId}`);
        const result = await response.json();
        
        if (!result.success) {
            throw new Error(result.error || 'Failed to load performance data');
        }
        
        const data = result.basicMetrics;
        const monthlyTrend = result.monthlyTrend || [];
        
        if (perfContent) {
            perfContent.innerHTML = `
                <div class="row">
                    <div class="col-md-6 mb-3">
                        <div class="bg-light p-3 rounded-3">
                            <small class="text-muted text-uppercase">Success Rate</small>
                            <h3 class="fw-bold mb-0">${data.successRate}%</h3>
                            <div class="progress mt-2" style="height: 6px;">
                                <div class="progress-bar bg-success" style="width: ${data.successRate}%;"></div>
                            </div>
                            <small class="text-muted">${data.deliveredOrders}/${data.totalOrders} delivered</small>
                        </div>
                    </div>
                    <div class="col-md-6 mb-3">
                        <div class="bg-light p-3 rounded-3">
                            <small class="text-muted text-uppercase">Avg Delivery Days</small>
                            <h3 class="fw-bold mb-0">${data.avgDeliveryDays} days</h3>
                            <small class="text-muted">Min: ${data.minDeliveryDays || '-'} | Max: ${data.maxDeliveryDays || '-'}</small>
                        </div>
                    </div>
                </div>
                <div class="row mt-2">
                    <div class="col-4">
                        <div class="text-center p-2">
                            <h5 class="fw-bold mb-0">${data.last7DaysOrders || 0}</h5>
                            <small class="text-muted">Last 7 Days</small>
                        </div>
                    </div>
                    <div class="col-4">
                        <div class="text-center p-2">
                            <h5 class="fw-bold mb-0">${data.last30DaysOrders || 0}</h5>
                            <small class="text-muted">Last 30 Days</small>
                        </div>
                    </div>
                    <div class="col-4">
                        <div class="text-center p-2">
                            <h5 class="fw-bold mb-0">${data.inTransitOrders || 0}</h5>
                            <small class="text-muted">In Transit</small>
                        </div>
                    </div>
                </div>
                ${monthlyTrend.length > 0 ? `
                    <div class="mt-3">
                        <h6 class="fw-bold mb-2">Monthly Trend</h6>
                        <div class="table-responsive">
                            <table class="table table-sm">
                                <thead>
                                    <tr>
                                        <th>Month</th>
                                        <th>Orders</th>
                                        <th>Success Rate</th>
                                        <th>Avg Days</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    ${monthlyTrend.map(m => `
                                        <tr>
                                            <td>${escapeHtml(m.monthName)}</td>
                                            <td>${m.totalOrders}</td>
                                            <td>${m.successRate}%</td>
                                            <td>${m.avgDeliveryDays} days</td>
                                        </tr>
                                    `).join('')}
                                </tbody>
                            </table>
                        </div>
                    </div>
                ` : ''}
            `;
        }
    } catch (error) {
        console.error('Error loading performance:', error);
        if (perfContent) {
            perfContent.innerHTML = `
                <div class="text-center py-4 text-danger">
                    <i class="bi bi-exclamation-triangle fs-1"></i>
                    <p>Failed to load performance data</p>
                </div>
            `;
        }
    }
}

// Refresh performance data
function refreshPerformance() {
    showToast('Refreshing performance metrics...', 'info');
    loadLogistics(currentFilter);
}

// Toast notification helper
function showToast(message, type = 'success') {
    const Toast = Swal.mixin({
        toast: true,
        position: 'top-end',
        showConfirmButton: false,
        timer: 3000,
        timerProgressBar: true
    });
    
    Toast.fire({
        icon: type,
        title: message
    });
}

// Helper function to escape HTML
function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Preview image before upload
function previewImage(input) {
    const preview = document.getElementById('logoPreview');
    if (input.files && input.files[0] && preview) {
        const reader = new FileReader();
        reader.onload = e => preview.src = e.target.result;
        reader.readAsDataURL(input.files[0]);
    }
}

// Handle status toggle (activate/inactivate)
async function handleStatusToggle(name, logisticsId, element, forceEnable) {
    const isActivating = forceEnable || (element && element.checked);
    const actionText = isActivating ? 'Enable' : 'Disable';
    const newStatus = isActivating ? 'Active' : 'Inactive';
    
    const { value: reason } = await Swal.fire({
        title: `${actionText} ${name}?`,
        text: `Please provide a reason for ${isActivating ? 'activating' : 'disabling'} this service:`,
        input: 'textarea',
        inputPlaceholder: 'e.g., Maintenance complete / Service quality issues',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: isActivating ? '#198754' : '#dc3545',
        cancelButtonColor: '#6c757d',
        confirmButtonText: `Confirm ${actionText}`,
        inputValidator: (value) => {
            if (!value) return 'A reason is required to proceed!';
        }
    });
    
    if (reason) {
        try {
            const response = await fetch('/Admin/UpdateLogisticsStatus', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    logisticsId: logisticsId,
                    status: newStatus,
                    reason: reason
                })
            });
            
            const result = await response.json();
            
            if (result.success) {
                showToast(result.message, 'success');
                loadLogisticsStats();
                loadLogistics(currentFilter);
            } else {
                showToast(result.message, 'error');
                if (element && element.type === 'checkbox') {
                    element.checked = !isActivating;
                }
            }
        } catch (error) {
            console.error('Error updating status:', error);
            showToast('Failed to update status', 'error');
            if (element && element.type === 'checkbox') {
                element.checked = !isActivating;
            }
        }
    } else if (element && element.type === 'checkbox') {
        element.checked = !isActivating;
    }
}

// Confirm delete/archive partner
async function confirmDeletePartner(logisticsId, name) {
    const { value: reason } = await Swal.fire({
        title: `Archive ${name}?`,
        html: `<p class="text-danger small">This courier will be archived and removed from seller options.</p>`,
        input: 'textarea',
        inputLabel: 'Reason for Archiving',
        inputPlaceholder: 'Enter the reason for archiving this partner...',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#6c757d',
        cancelButtonColor: '#dc3545',
        confirmButtonText: 'Archive Partner',
        inputValidator: (value) => {
            if (!value) return 'You must provide a reason for archiving!';
        }
    });
    
    if (reason) {
        try {
            const response = await fetch('/Admin/DeleteLogistics', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    logisticsId: logisticsId,
                    reason: reason
                })
            });
            
            const result = await response.json();
            
            if (result.success) {
                showToast(result.message, 'success');
                loadLogisticsStats();
                loadLogistics(currentFilter);
            } else {
                showToast(result.message, 'error');
            }
        } catch (error) {
            console.error('Error archiving partner:', error);
            showToast('Failed to archive partner', 'error');
        }
    }
}

// Restore partner from archive
async function restorePartner(logisticsId, name) {
    const { value: reason } = await Swal.fire({
        title: 'Restore Partner?',
        text: `Enter the reason for bringing ${name} back to service:`,
        input: 'text',
        inputPlaceholder: 'e.g., Contract renewed / Service restored',
        icon: 'question',
        showCancelButton: true,
        confirmButtonColor: '#198754',
        cancelButtonColor: '#6c757d',
        confirmButtonText: 'Yes, Restore Partner',
        inputValidator: (value) => {
            if (!value) return 'You must provide a reason to restore this partner!';
        }
    });
    
    if (reason) {
        try {
            const response = await fetch('/Admin/RestoreLogistics', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    logisticsId: logisticsId,
                    reason: reason
                })
            });
            
            const result = await response.json();
            
            if (result.success) {
                showToast(result.message, 'success');
                loadLogisticsStats();
                loadLogistics('active');
                filterPartners('active');
            } else {
                showToast(result.message, 'error');
            }
        } catch (error) {
            console.error('Error restoring partner:', error);
            showToast('Failed to restore partner', 'error');
        }
    }
}