// Global variables
let currentChallengeId = null;
let isEditMode = false;
let prizeTierCount = 0;
let currentPrizesData = [];
let currentParticipantsData = [];
let currentChallengeFilter = null;
let currentProofImageData = null;
let currentProofFileName = null;
let currentProofFileType = null;

// Load data on page load
document.addEventListener('DOMContentLoaded', function() {
    loadChallengeStatistics();
    loadAllChallenges();
    loadLeaderboard();
    loadChallengeFilters();

    // Search functionality for leaderboard
    const searchInput = document.getElementById('leaderboardSearch');
    if (searchInput) {
        searchInput.addEventListener('keyup', filterLeaderboardBySearch);
    }
    
    // Prize search functionality with debounce
    const prizeSearchInput = document.getElementById('prizeSearchInput');
    if (prizeSearchInput) {
        prizeSearchInput.addEventListener('keyup', handlePrizeSearch);
    }
    
    // Prize status filter change
    const prizeStatusFilter = document.getElementById('prizeStatusFilter');
    if (prizeStatusFilter) {
        prizeStatusFilter.addEventListener('change', function() {
            loadPrizes();
        });
    }
    
    // Load prizes if the prizes tab is active
    const prizesView = document.getElementById('view-prizes');
    if (prizesView && !prizesView.classList.contains('d-none')) {
        loadPrizes();
    }

    const totalAthletesCard = document.getElementById('totalAthletesCard');
    if (totalAthletesCard) {
        console.log('Total Athletes card found, attaching click handler');
        totalAthletesCard.style.cursor = 'pointer';
        totalAthletesCard.addEventListener('click', function(e) {
            console.log('Total Athletes card clicked');
            e.stopPropagation();
            loadAllParticipants();
        });
    } else {
        console.log('Total Athletes card not found');
    }
});


async function loadAllParticipants(challengeId = null) {
    currentChallengeFilter = challengeId;
    
    const participantsBody = document.getElementById('participantsTableBody');
    if (!participantsBody) return;
    
    // Show loading state
    participantsBody.innerHTML = '<tr><td colspan="6" class="text-center py-5"><div class="spinner-border text-primary" role="status"></div><p class="mt-2 text-muted">Loading participants...</p></td></tr>';
    
    try {
        let url = '/Admin/GetAllParticipants';
        if (challengeId) {
            url += `?challengeId=${challengeId}`;
        }
        
        const response = await fetch(url);
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const result = await response.json();
        
        if (result.error) {
            throw new Error(result.error);
        }
        
        currentParticipantsData = result.participants || [];
        renderParticipantsTable(currentParticipantsData);
        
        // Show modal
        const modal = new bootstrap.Modal(document.getElementById('participantsModal'));
        modal.show();
        
    } catch (error) {
        console.error('Error loading participants:', error);
        participantsBody.innerHTML = '<tr><td colspan="6" class="text-center py-5 text-danger">Error loading participants: ' + error.message + '</td></tr>';
        showToast('Error loading participants: ' + error.message, true);
    }
}

function renderParticipantsTable(participants) {
    const participantsBody = document.getElementById('participantsTableBody');
    if (!participantsBody) return;
    
    if (!participants || participants.length === 0) {
        participantsBody.innerHTML = '<tr><td colspan="6" class="text-center py-5 text-muted">No participants found</td></tr>';
        return;
    }
    
    participantsBody.innerHTML = '';
    
    participants.forEach(participant => {
        const statusClass = participant.status === 'Approved' ? 'bg-success' : 
                           (participant.status === 'Pending' ? 'bg-warning' : 'bg-danger');
        
        const row = document.createElement('tr');
        row.innerHTML = `
            <td>
                <div class="d-flex align-items-center gap-2">
                    <img src="${participant.avatarUrl || 'https://ui-avatars.com/api/?name=' + encodeURIComponent(participant.athleteName)}" 
                         class="rounded-circle" width="32" height="32"
                         onerror="this.src='https://ui-avatars.com/api/?name=${encodeURIComponent(participant.athleteName)}'">
                    <div>
                        <div class="fw-bold">${escapeHtml(participant.athleteName)}</div>
                        <small class="text-muted">${escapeHtml(participant.email || '')}</small>
                    </div>
                </div>
            </td>
            <td>
                <span class="fw-semibold">${escapeHtml(participant.challengeTitle)}</span>
                <br>
                <small class="text-muted">Joined: ${formatDate(participant.joinedAt)}</small>
            </td>
            <td>
                <strong>${(participant.totalDistanceKm || 0).toFixed(1)} km</strong>
                <br>
                <small class="text-muted">Goal: ${(participant.goalKm || 0).toFixed(1)} km</small>
            </td>
            <td>${participant.totalActivities || 0}</td>
            <td>
                <span class="badge ${statusClass} rounded-pill px-3 py-1 participant-status-badge">
                    ${participant.status || 'Pending'}
                </span>
            </td>
            <td class="action-buttons">
                ${participant.status === 'Pending' ? `
                    <button class="btn btn-sm btn-success rounded-pill me-1" onclick="openUpdateStatusModal(${participant.participantId}, ${participant.challengeId}, '${escapeHtml(participant.athleteName)}', '${escapeHtml(participant.challengeTitle)}', '${participant.status}')">
                        <i class="bi bi-check-lg"></i> Approve
                    </button>
                    <button class="btn btn-sm btn-danger rounded-pill" onclick="openUpdateStatusModal(${participant.participantId}, ${participant.challengeId}, '${escapeHtml(participant.athleteName)}', '${escapeHtml(participant.challengeTitle)}', '${participant.status}')">
                        <i class="bi bi-x-lg"></i> Reject
                    </button>
                ` : participant.status === 'Approved' ? `
                    <button class="btn btn-sm btn-warning rounded-pill" onclick="openUpdateStatusModal(${participant.participantId}, ${participant.challengeId}, '${escapeHtml(participant.athleteName)}', '${escapeHtml(participant.challengeTitle)}', '${participant.status}')">
                        <i class="bi bi-pencil"></i> Change Status
                    </button>
                ` : `
                    <button class="btn btn-sm btn-secondary rounded-pill" onclick="openUpdateStatusModal(${participant.participantId}, ${participant.challengeId}, '${escapeHtml(participant.athleteName)}', '${escapeHtml(participant.challengeTitle)}', '${participant.status}')">
                        <i class="bi bi-arrow-repeat"></i> Reconsider
                    </button>
                `}
            </td>
        `;
        
        participantsBody.appendChild(row);
    });
}


// Filter participants by status and search
function filterParticipants() {
    const statusFilter = document.getElementById('participantStatusFilter')?.value || 'all';
    const searchTerm = document.getElementById('participantSearchInput')?.value.toLowerCase() || '';
    
    let filtered = [...currentParticipantsData];
    
    // Filter by status
    if (statusFilter !== 'all') {
        filtered = filtered.filter(p => p.status === statusFilter);
    }
    
    // Filter by search term
    if (searchTerm) {
        filtered = filtered.filter(p => 
            p.athleteName.toLowerCase().includes(searchTerm) ||
            (p.email && p.email.toLowerCase().includes(searchTerm))
        );
    }
    
    renderParticipantsTable(filtered);
}

// Reset participant filters
function resetParticipantFilters() {
    const statusFilter = document.getElementById('participantStatusFilter');
    const searchInput = document.getElementById('participantSearchInput');
    
    if (statusFilter) statusFilter.value = 'all';
    if (searchInput) searchInput.value = '';
    
    filterParticipants();
}

// Open update status modal
function openUpdateStatusModal(participantId, challengeId, athleteName, challengeTitle, currentStatus) {
    document.getElementById('updateParticipantId').value = participantId;
    document.getElementById('updateChallengeId').value = challengeId;
    document.getElementById('updateAthleteName').innerText = athleteName;
    document.getElementById('updateChallengeTitle').innerText = challengeTitle;
    
    const currentStatusSpan = document.getElementById('updateCurrentStatus');
    const statusClass = currentStatus === 'Approved' ? 'bg-success' : 
                       (currentStatus === 'Pending' ? 'bg-warning' : 'bg-danger');
    currentStatusSpan.className = `badge ${statusClass}`;
    currentStatusSpan.innerText = currentStatus;
    
    const newStatusSelect = document.getElementById('newStatusSelect');
    newStatusSelect.value = currentStatus;
    
    // Show/hide rejection reason based on selected status
    const rejectionReasonDiv = document.getElementById('rejectionReasonDiv');
    const rejectionReason = document.getElementById('rejectionReason');
    
    newStatusSelect.onchange = function() {
        if (this.value === 'Rejected') {
            rejectionReasonDiv.style.display = 'block';
        } else {
            rejectionReasonDiv.style.display = 'none';
            rejectionReason.value = '';
        }
    };
    
    if (currentStatus === 'Rejected') {
        rejectionReasonDiv.style.display = 'block';
    } else {
        rejectionReasonDiv.style.display = 'none';
        rejectionReason.value = '';
    }
    
    const modal = new bootstrap.Modal(document.getElementById('updateStatusModal'));
    modal.show();
}

// Submit status update
async function submitStatusUpdate() {
    const participantId = document.getElementById('updateParticipantId').value;
    const challengeId = document.getElementById('updateChallengeId').value;
    const newStatus = document.getElementById('newStatusSelect').value;
    const rejectionReason = document.getElementById('rejectionReason').value;
    
    if (newStatus === 'Rejected' && !rejectionReason) {
        showToast('Please provide a reason for rejection', true);
        return;
    }
    
    try {
        const response = await fetch('/Admin/UpdateParticipantStatus', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                participantId: parseInt(participantId),
                challengeId: parseInt(challengeId),
                status: newStatus,
                rejectionReason: rejectionReason
            })
        });
        
        const result = await response.json();
        
        if (result.success) {
            showToast(result.message);
            
            // Close the update modal
            const updateModal = bootstrap.Modal.getInstance(document.getElementById('updateStatusModal'));
            updateModal.hide();
            
            // Refresh the participants list
            await loadAllParticipants(currentChallengeFilter);
        } else {
            showToast(result.message || 'Error updating status', true);
        }
    } catch (error) {
        console.error('Error updating status:', error);
        showToast('Error updating participant status', true);
    }
}

let searchTimeout;
function handlePrizeSearch() {
    clearTimeout(searchTimeout);
    searchTimeout = setTimeout(() => {
        const searchTerm = document.getElementById('prizeSearchInput')?.value.trim() || '';
        if (searchTerm.length === 0 || searchTerm.length >= 3) {
            loadPrizes();
        }
    }, 500);
}

// Load challenge statistics
async function loadChallengeStatistics() {
    const totalAthletes = document.getElementById('totalAthletes');
    const activeChallenges = document.getElementById('activeChallenges');
    const avgDistance = document.getElementById('avgDistance');
    const totalTime = document.getElementById('totalTime');
    
    if (!totalAthletes || !activeChallenges || !avgDistance || !totalTime) return;
    
    try {
        const response = await fetch('/Admin/GetChallengeStatistics');
        const stats = await response.json();
        
        totalAthletes.innerText = (stats.totalAthletes || 0).toLocaleString();
        activeChallenges.innerText = stats.activeChallenges || '0';
        avgDistance.innerText = (stats.avgDistance?.toFixed(1) || '0') + ' km';
        totalTime.innerText = Math.round(stats.totalTimeHours || 0) + ' h';
    } catch (error) {
        console.error('Error loading statistics:', error);
    }
}

async function loadPrizes() {
    const statusFilter = document.getElementById('prizeStatusFilter')?.value || 'all';
    const searchCode = document.getElementById('prizeSearchInput')?.value || '';
    const prizesBody = document.getElementById('prizesTableBody');
    
    if (!prizesBody) return;
    
    // Show loading state
    prizesBody.innerHTML = '<tr><td colspan="6" class="text-center py-5"><div class="spinner-border text-primary" role="status"></div><p class="mt-2 text-muted">Loading prizes...</p></td>' + '</tr>';
    
    try {
        let url = `/Admin/GetAllPrizes?status=${encodeURIComponent(statusFilter)}`;
        if (searchCode) {
            url += `&searchCode=${encodeURIComponent(searchCode)}`;
        }
        
        const response = await fetch(url);
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const result = await response.json();
        
        if (result.error) {
            throw new Error(result.error);
        }
        
        currentPrizesData = result.prizes || [];
        renderPrizesTable(currentPrizesData);
        
    } catch (error) {
        console.error('Error loading prizes:', error);
        prizesBody.innerHTML = '<tr><td colspan="6" class="text-center py-5 text-danger">Error loading prizes: ' + error.message + '</td>' + '</tr>';
        showToast('Error loading prizes: ' + error.message, true);
    }
}

function renderPrizesTable(prizes) {
    const prizesBody = document.getElementById('prizesTableBody');
    if (!prizesBody) return;
    
    if (!prizes || prizes.length === 0) {
        prizesBody.innerHTML = '<tr><td colspan="6" class="text-center py-5 text-muted">No prizes found</td></tr>';
        return;
    }
    
    prizesBody.innerHTML = '';
    
    prizes.forEach(prize => {
        const statusClass = prize.claimStatus === 'Claimed' ? 'bg-success' : 'bg-warning';
        const statusText = prize.claimStatus === 'Claimed' ? 'Claimed' : 'Unclaimed';
        
        // Format prize description based on type
        let prizeDescription = '';
        if (prize.cashAmount && prize.cashAmount > 0) {
            prizeDescription = `₱${parseFloat(prize.cashAmount).toLocaleString()} Cash`;
        } else if (prize.voucherDiscountPercent) {
            prizeDescription = `${prize.voucherDiscountPercent}% Off Voucher`;
        } else if (prize.voucherDiscountFixed) {
            prizeDescription = `₱${parseFloat(prize.voucherDiscountFixed).toLocaleString()} Off Voucher`;
        } else if (prize.rewardName) {
            prizeDescription = prize.rewardName;
        } else {
            prizeDescription = prize.description || 'Prize';
        }
        
        const row = document.createElement('tr');
        row.innerHTML = `
            <td class="fw-semibold">
                <div class="d-flex align-items-center gap-2">
                    <img src="${prize.avatarUrl || 'https://ui-avatars.com/api/?name=' + encodeURIComponent(prize.winnerName)}" 
                         class="rounded-circle" width="32" height="32" 
                         onerror="this.src='https://ui-avatars.com/api/?name=${encodeURIComponent(prize.winnerName)}'">
                    <div>
                        <div class="fw-bold">${escapeHtml(prize.winnerName)}</div>
                        <small class="text-muted">Rank #${prize.rankPosition}</small>
                    </div>
                </div>
            </td>
            <td>
                <span class="fw-semibold">${escapeHtml(prizeDescription)}</span>
                <br>
                <small class="text-muted">${escapeHtml(prize.challengeTitle || '')}</small>
            </td>
            <td>
                <span class="badge bg-light text-dark border rounded-pill px-3 py-1">
                    ${escapeHtml(prize.prizeType || 'Prize')}
                </span>
                ${prize.tierName ? `<br><small class="text-muted">${escapeHtml(prize.tierName)}</small>` : ''}
            </td>
            <td>
                <code class="bg-light px-2 py-1 rounded" style="font-size: 0.75rem;">${escapeHtml(prize.claimCode || 'N/A')}</code>
                <button class="btn btn-sm btn-link text-primary p-0 ms-2" onclick="event.stopPropagation(); copyClaimCode('${prize.claimCode}')" title="Copy code">
                    <i class="bi bi-clipboard"></i>
                </button>
            </td>
            <td>
                <span class="badge ${statusClass} rounded-pill px-3 py-1">${statusText}</span>
            </td>
            <td>
                <small class="text-muted">${prize.claimDeadline ? formatDate(prize.claimDeadline) : 'No deadline'}</small>
                ${prize.claimStatus === 'Pending' && prize.claimDeadline ? 
                  `<br><small class="text-danger">${getDaysLeft(prize.claimDeadline)} days left</small>` : ''}
            </td>
        `;
        
        // Add click handler to view details
        row.style.cursor = 'pointer';
        row.onclick = () => viewPrizeDetails(prize);
        
        prizesBody.appendChild(row);
    });
}


// Filter prizes by claim code
function filterPrizesByCode() {
    const searchTerm = document.getElementById('prizeSearchInput')?.value.trim() || '';
    
    if (searchTerm.length === 0) {
        loadPrizes();
    } else if (searchTerm.length >= 3) { // Only search after 3 characters
        loadPrizes();
    }
}


// Reset all prize filters
function resetPrizeFilters() {
    const statusFilter = document.getElementById('prizeStatusFilter');
    const searchInput = document.getElementById('prizeSearchInput');
    
    if (statusFilter) statusFilter.value = 'all';
    if (searchInput) searchInput.value = '';
    
    loadPrizes();
}

// Copy claim code to clipboard
function copyClaimCode(code) {
    if (!code) return;
    
    navigator.clipboard.writeText(code).then(() => {
        showToast('Claim code copied to clipboard!');
    }).catch(() => {
        // Fallback for older browsers
        const textarea = document.createElement('textarea');
        textarea.value = code;
        document.body.appendChild(textarea);
        textarea.select();
        document.execCommand('copy');
        document.body.removeChild(textarea);
        showToast('Claim code copied to clipboard!');
    });
}

// Get days left until deadline
function getDaysLeft(deadlineDate) {
    if (!deadlineDate) return 0;
    const deadline = new Date(deadlineDate);
    const today = new Date();
    const diffTime = deadline - today;
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
    return diffDays > 0 ? diffDays : 0;
}

// View prize details in modal
function viewPrizeDetails(prize) {
    // Create or get modal
    let modal = document.getElementById('prizeDetailsModal');
    if (!modal) {
        modal = document.createElement('div');
        modal.className = 'modal fade';
        modal.id = 'prizeDetailsModal';
        modal.tabIndex = -1;
        modal.innerHTML = `
            <div class="modal-dialog modal-dialog-centered modal-lg">
                <div class="modal-content rounded-4">
                    <div class="modal-header border-0">
                        <h5 class="modal-title fw-bold">
                            <i class="bi bi-gift-fill text-primary me-2"></i>Prize Details
                        </h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body" id="prizeDetailsContent" style="max-height: 70vh; overflow-y: auto;">
                    </div>
                    <div class="modal-footer border-0">
                        <button type="button" class="btn btn-secondary rounded-pill" data-bs-dismiss="modal">Close</button>
                    </div>
                </div>
            </div>
        `;
        document.body.appendChild(modal);
    }
    
    const content = document.getElementById('prizeDetailsContent');
    if (content) {
        // Format prize value
        let prizeValue = '';
        if (prize.cashAmount && prize.cashAmount > 0) {
            prizeValue = `₱${parseFloat(prize.cashAmount).toLocaleString()}`;
        } else if (prize.voucherDiscountPercent) {
            prizeValue = `${prize.voucherDiscountPercent}% OFF`;
        } else if (prize.voucherDiscountFixed) {
            prizeValue = `₱${parseFloat(prize.voucherDiscountFixed).toLocaleString()} OFF`;
        } else if (prize.rewardValue) {
            prizeValue = `₱${parseFloat(prize.rewardValue).toLocaleString()}`;
        }
        
        // Show different content based on claim status
        const isClaimed = prize.claimStatus === 'Claimed';
        
        // Build proof image display for claimed prizes
        let proofImageHtml = '';
        if (isClaimed && prize.proofImageBase64) {
            if (prize.proofContentType && prize.proofContentType === 'application/pdf') {
                proofImageHtml = `
                    <div class="mb-3">
                        <label class="text-muted small fw-semibold">Proof of Claim</label>
                        <div class="border rounded-3 p-3 text-center bg-light">
                            <i class="bi bi-file-pdf fs-1 text-danger"></i>
                            <p class="mb-1">${escapeHtml(prize.proofFileName || 'Proof Document')}</p>
                            <a href="${prize.proofImageBase64}" download="${escapeHtml(prize.proofFileName || 'proof.pdf')}" 
                               class="btn btn-sm btn-outline-primary mt-2">
                                <i class="bi bi-download"></i> Download PDF
                            </a>
                            <button class="btn btn-sm btn-outline-secondary mt-2 ms-2" onclick="viewProofDocument('${prize.proofImageBase64}', '${prize.proofContentType}')">
                                <i class="bi bi-eye"></i> View Document
                            </button>
                        </div>
                    </div>
                `;
            } else if (prize.proofImageBase64) {
                proofImageHtml = `
                    <div class="mb-3">
                        <label class="text-muted small fw-semibold">Proof of Claim</label>
                        <div class="border rounded-3 p-2 text-center bg-light">
                            <img src="${prize.proofImageBase64}" class="img-fluid rounded-3" style="max-height: 200px; cursor: pointer;" 
                                 onclick="viewFullImage('${prize.proofImageBase64}')" title="Click to view full size">
                            <p class="small text-muted mt-1">${escapeHtml(prize.proofFileName || 'Proof Image')}</p>
                            <a href="${prize.proofImageBase64}" download="${escapeHtml(prize.proofFileName || 'proof.jpg')}" 
                               class="btn btn-sm btn-outline-primary">
                                <i class="bi bi-download"></i> Download
                            </a>
                        </div>
                    </div>
                `;
            }
        }
        
        content.innerHTML = `
            <div class="mb-3">
                <label class="text-muted small">Winner</label>
                <div class="d-flex align-items-center gap-2 mt-1">
                    <div class="rounded-circle bg-secondary d-flex align-items-center justify-content-center text-white" 
                         style="width: 40px; height: 40px; font-size: 18px;">
                        ${prize.winnerName ? prize.winnerName.charAt(0).toUpperCase() : '?'}
                    </div>
                    <div>
                        <p class="fw-semibold mb-0">${escapeHtml(prize.winnerName)}</p>
                        <small class="text-muted">Rank #${prize.rankPosition}</small>
                    </div>
                </div>
            </div>
            <div class="mb-3">
                <label class="text-muted small">Challenge</label>
                <p class="mb-0">${escapeHtml(prize.challengeTitle || 'N/A')}</p>
            </div>
            <div class="mb-3">
                <label class="text-muted small">Prize Type</label>
                <p class="mb-0">
                    <span class="badge bg-light text-dark border">${escapeHtml(prize.prizeType || 'Prize')}</span>
                    ${prize.tierName ? `<span class="badge bg-secondary ms-2">${escapeHtml(prize.tierName)}</span>` : ''}
                </p>
            </div>
            <div class="mb-3">
                <label class="text-muted small">Prize Description</label>
                <p class="mb-0">${escapeHtml(prize.description || 'N/A')}</p>
                ${prizeValue ? `<p class="fw-bold text-primary mt-1">${prizeValue}</p>` : ''}
            </div>
            <div class="mb-3">
                <label class="text-muted small">Claim Code</label>
                <div class="input-group">
                    <code class="form-control bg-light">${escapeHtml(prize.claimCode || 'N/A')}</code>
                    <button class="btn btn-outline-primary" onclick="copyClaimCode('${prize.claimCode}')">
                        <i class="bi bi-clipboard"></i> Copy
                    </button>
                </div>
            </div>
            <div class="row">
                <div class="col-md-6 mb-3">
                    <label class="text-muted small">Claim Status</label>
                    <p><span class="badge ${prize.claimStatus === 'Claimed' ? 'bg-success' : 'bg-warning'} rounded-pill px-3 py-1">${prize.claimStatus || 'Pending'}</span></p>
                </div>
                <div class="col-md-6 mb-3">
                    <label class="text-muted small">Claim Deadline</label>
                    <p class="mb-0 ${prize.claimStatus === 'Pending' && prize.claimDeadline ? 'text-danger fw-bold' : ''}">
                        ${prize.claimDeadline ? formatDate(prize.claimDeadline) : 'No deadline'}
                        ${prize.claimStatus === 'Pending' && prize.claimDeadline ? `<br><small>${getDaysLeft(prize.claimDeadline)} days remaining</small>` : ''}
                    </p>
                </div>
            </div>
            ${prize.claimDate ? `
            <div class="mb-3">
                <label class="text-muted small">Claim Date</label>
                <p class="mb-0">${formatDate(prize.claimDate)}</p>
            </div>
            ` : ''}
            <div class="mb-3">
                <label class="text-muted small">Awarded On</label>
                <p class="mb-0">${formatDate(prize.createdAt)}</p>
            </div>
            
            ${prize.claimNotes ? `
            <div class="mb-3">
                <label class="text-muted small">Claim Notes</label>
                <p class="mb-0 bg-light p-2 rounded-3">${escapeHtml(prize.claimNotes)}</p>
            </div>
            ` : ''}
            
            ${proofImageHtml}
            
            ${!isClaimed ? `
            <hr class="my-3">
            <div class="claim-prize-section">
                <h6 class="fw-bold mb-3"><i class="bi bi-trophy text-warning me-2"></i>Claim Your Prize</h6>
                <div class="mb-3">
                    <label class="form-label fw-semibold">Proof of Image / Receipt</label>
                    <div class="proof-upload-container border rounded-3 p-3 text-center" 
                         style="cursor: pointer; background-color: #f8f9fa; border-style: dashed !important;"
                         onclick="document.getElementById('proofImageInput').click()">
                        <i class="bi bi-cloud-upload fs-1 text-muted"></i>
                        <p class="mb-0 text-muted">Click to upload proof image</p>
                        <small class="text-muted">Supported formats: JPG, PNG, PDF (Max 5MB)</small>
                    </div>
                    <input type="file" id="proofImageInput" class="d-none" accept="image/*,application/pdf" onchange="previewProofImage(this)">
                    <div id="proofImagePreview" class="mt-2 d-none">
                        <img id="proofPreviewImg" class="img-fluid rounded-3" style="max-height: 150px;">
                        <button type="button" class="btn btn-sm btn-outline-danger mt-1" onclick="clearProofImage()">
                            <i class="bi bi-trash"></i> Remove
                        </button>
                    </div>
                    <div id="proofPdfPreview" class="mt-2 d-none">
                        <div class="border rounded-3 p-2 text-center">
                            <i class="bi bi-file-pdf fs-1 text-danger"></i>
                            <p id="pdfFileName" class="mb-0 small"></p>
                            <button type="button" class="btn btn-sm btn-outline-danger mt-1" onclick="clearProofImage()">
                                <i class="bi bi-trash"></i> Remove
                            </button>
                        </div>
                    </div>
                </div>
                <div class="mb-3">
                    <label class="form-label fw-semibold">Additional Notes (Optional)</label>
                    <textarea id="claimNotes" class="form-control rounded-3" rows="2" 
                              placeholder="Any additional information for your claim..."></textarea>
                </div>
                <button class="btn btn-primary rounded-pill w-100" onclick="submitClaimPrize('${prize.claimCode}', ${prize.winnerId})">
                    <i class="bi bi-gift me-2"></i>Claim Prize
                </button>
            </div>
            ` : ''}
        `;
    }
    
    const bsModal = new bootstrap.Modal(modal);
    bsModal.show();
}

// Function to view full image
function viewFullImage(imageUrl) {
    let modal = document.getElementById('fullImageModal');
    if (!modal) {
        modal = document.createElement('div');
        modal.className = 'modal fade';
        modal.id = 'fullImageModal';
        modal.tabIndex = -1;
        modal.innerHTML = `
            <div class="modal-dialog modal-dialog-centered modal-lg">
                <div class="modal-content rounded-4">
                    <div class="modal-header border-0">
                        <h5 class="modal-title fw-bold">Proof of Claim</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body text-center">
                        <img id="fullImageViewer" src="" class="img-fluid rounded-3" style="max-height: 70vh;">
                    </div>
                    <div class="modal-footer border-0">
                        <button type="button" class="btn btn-secondary rounded-pill" data-bs-dismiss="modal">Close</button>
                    </div>
                </div>
            </div>
        `;
        document.body.appendChild(modal);
    }
    
    const fullImageViewer = document.getElementById('fullImageViewer');
    if (fullImageViewer) {
        fullImageViewer.src = imageUrl;
    }
    
    const bsModal = new bootstrap.Modal(modal);
    bsModal.show();
}

// Function to view proof document (PDF)
function viewProofDocument(documentUrl, contentType) {
    if (contentType === 'application/pdf') {
        // Open PDF in new tab
        const pdfWindow = window.open();
        pdfWindow.document.write(`
            <html>
                <head><title>Proof Document</title></head>
                <body style="margin:0; padding:0;">
                    <embed src="${documentUrl}" width="100%" height="100%" type="application/pdf">
                </body>
            </html>
        `);
    } else {
        // Open image in modal
        viewFullImage(documentUrl);
    }
}

// Submit claim prize
async function submitClaimPrize(claimCode, winnerId) {
    if (!claimCode) {
        showToast('Invalid claim code', true);
        return;
    }
    
    // Validate proof image
    if (!currentProofImageData) {
        showToast('Please upload proof of your prize claim', true);
        return;
    }
    
    const notes = document.getElementById('claimNotes')?.value || '';
    
    // Show loading state
    const claimButton = document.querySelector('#prizeDetailsModal .btn-primary');
    const originalText = claimButton.innerHTML;
    claimButton.disabled = true;
    claimButton.innerHTML = '<i class="bi bi-hourglass-split me-2"></i>Processing...';
    
    try {
        const requestBody = {
            claimCode: claimCode,
            winnerId: winnerId,
            proofImage: currentProofImageData,
            proofFileName: currentProofFileName,
            proofContentType: currentProofFileType,
            notes: notes
        };
        
        const response = await fetch('/Admin/ClaimPrizeWithProof', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(requestBody)
        });
        
        const result = await response.json();
        
        if (result.success) {
            showToast(result.message || 'Prize claimed successfully!');
            
            // Close the modal
            const modal = bootstrap.Modal.getInstance(document.getElementById('prizeDetailsModal'));
            if (modal) modal.hide();
            
            // Refresh the prizes list
            loadPrizes();
            
            // Clear proof data
            clearProofImage();
        } else {
            showToast(result.message || 'Error claiming prize', true);
        }
    } catch (error) {
        console.error('Error claiming prize:', error);
        showToast('Error claiming prize. Please try again.', true);
    } finally {
        claimButton.disabled = false;
        claimButton.innerHTML = originalText;
    }
}

function clearProofImage() {
    const proofInput = document.getElementById('proofImageInput');
    const previewDiv = document.getElementById('proofImagePreview');
    const pdfPreviewDiv = document.getElementById('proofPdfPreview');
    
    if (proofInput) proofInput.value = '';
    if (previewDiv) previewDiv.classList.add('d-none');
    if (pdfPreviewDiv) pdfPreviewDiv.classList.add('d-none');
    
    currentProofImageData = null;
    currentProofFileName = null;
    currentProofFileType = null;
}

// Preview proof image before upload
function previewProofImage(input) {
    const previewDiv = document.getElementById('proofImagePreview');
    const pdfPreviewDiv = document.getElementById('proofPdfPreview');
    const previewImg = document.getElementById('proofPreviewImg');
    const pdfFileName = document.getElementById('pdfFileName');
    
    if (input.files && input.files[0]) {
        const file = input.files[0];
        
        // Validate file size (max 5MB)
        if (file.size > 5 * 1024 * 1024) {
            showToast('File size must be less than 5MB', true);
            input.value = '';
            return;
        }
        
        currentProofFileName = file.name;
        currentProofFileType = file.type;
        
        if (file.type === 'application/pdf') {
            // Handle PDF
            previewDiv.classList.add('d-none');
            pdfPreviewDiv.classList.remove('d-none');
            pdfFileName.innerText = file.name;
            
            // Read PDF as base64
            const reader = new FileReader();
            reader.onload = function(e) {
                currentProofImageData = e.target.result;
            };
            reader.readAsDataURL(file);
        } else if (file.type.startsWith('image/')) {
            // Handle Image
            pdfPreviewDiv.classList.add('d-none');
            previewDiv.classList.remove('d-none');
            
            const reader = new FileReader();
            reader.onload = function(e) {
                previewImg.src = e.target.result;
                currentProofImageData = e.target.result;
            };
            reader.readAsDataURL(file);
        } else {
            showToast('Please upload an image or PDF file', true);
            input.value = '';
        }
    }
}

// Load all challenges
async function loadAllChallenges() {
    const container = document.getElementById('challengesList');
    if (!container) return;
    
    try {
        const response = await fetch('/Admin/GetAllChallenges');
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const challenges = await response.json();
        
        if (!Array.isArray(challenges) || challenges.length === 0) {
            container.innerHTML = '<div class="col-12 text-center py-5">No challenges found</div>';
            return;
        }
        
        container.innerHTML = '';
        challenges.forEach(challenge => {
            const statusClass = challenge.status === 'Live' ? 'bg-success' : 
                               (challenge.status === 'Upcoming' ? 'bg-warning' : 'bg-secondary');
            
            // Handle null/undefined bannerBase64
            const bannerImage = challenge.bannerBase64 && challenge.bannerBase64 !== 'null' ? 
                               challenge.bannerBase64 : '/images/challenge-placeholder.jpg';
            
            const card = document.createElement('div');
            card.className = 'col-md-6 col-lg-4';
            card.innerHTML = `
                <div class="card border-0 shadow-sm rounded-4 overflow-hidden h-100 challenge-card-hover"
                     onclick="window.location.href='/Admin/ChallengeDetails/${challenge.challengeId}'" style="cursor:pointer;">
                    <div class="position-relative">
                        <img src="${bannerImage}" 
                             class="card-img-top" style="height: 150px; object-fit: cover;" 
                             onerror="this.src='/images/challenge-placeholder.jpg'">
                        <span class="position-absolute top-0 end-0 m-2 badge ${statusClass} shadow-sm">${challenge.status}</span>
                    </div>
                    <div class="p-3">
                        <h6 class="fw-bold mb-0 text-dark">${escapeHtml(challenge.title)}</h6>
                        <p class="text-muted mb-2" style="font-size: 0.75rem;">${formatDate(challenge.startDate)} - ${formatDate(challenge.endDate)}</p>
                        <div class="progress mb-2" style="height: 4px;">
                            <div class="progress-bar bg-success" style="width: ${challenge.completionRate || 0}%"></div>
                        </div>
                        <div class="d-flex justify-content-between align-items-center mt-2">
                            <span class="small text-primary fw-bold">View Details <i class="bi bi-arrow-right"></i></span>
                            <span class="text-muted small"><i class="bi bi-people"></i> ${challenge.totalParticipants || 0}</span>
                        </div>
                    </div>
                </div>
            `;
            container.appendChild(card);
        });
    } catch (error) {
        console.error('Error loading challenges:', error);
        container.innerHTML = '<div class="col-12 text-center py-5 text-danger">Error loading challenges</div>';
        showToast('Error loading challenges: ' + error.message, true);
    }
}

// Global variables
let currentLeaderboardData = [];
let selectedChallengeId = null;

// Load leaderboard (global or filtered by challenge)
async function loadLeaderboard() {
    const leaderboardBody = document.getElementById('leaderboardBody');
    if (!leaderboardBody) return;
    
    try {
        let url = '/Admin/GetGlobalLeaderboard';
        if (selectedChallengeId && selectedChallengeId !== 'all') {
            url += `?challengeId=${selectedChallengeId}`;
        }
        
        console.log('Fetching leaderboard from:', url);
        const response = await fetch(url);
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const data = await response.json();
        
        // Check for error response
        if (data.error) {
            throw new Error(data.error);
        }
        
        currentLeaderboardData = data;
        
        if (!Array.isArray(data) || data.length === 0) {
            leaderboardBody.innerHTML = '<tr><td colspan="8" class="text-center py-4">No participants yet</td></tr>';
            return;
        }
        
        leaderboardBody.innerHTML = '';
        
        data.forEach(participant => {
            const progressPercent = participant.progressPercent || 0;
            const row = document.createElement('tr');
            row.innerHTML = `
                <td class="fw-bold">#${participant.globalRank}</td>
                <td class="text-start ps-4">
                    <div class="d-flex align-items-center gap-2" style="cursor:pointer" onclick="openUserView('${escapeHtml(participant.athleteName)}', ${participant.globalRank}, ${participant.totalDistanceKm}, ${participant.totalActivities}, '${participant.totalTimeFormatted}', '${participant.avatarUrl}')">
                        <img src="${participant.avatarUrl || 'https://ui-avatars.com/api/?name=' + encodeURIComponent(participant.athleteName)}" class="rounded-circle border" width="30" height="30" onerror="this.src='https://ui-avatars.com/api/?name=${encodeURIComponent(participant.athleteName)}'">
                        <span class="fw-bold text-dark">${escapeHtml(participant.athleteName)}</span>
                    </div>
                </td>
                <td class="text-start">
                    <div>
                        <span class="fw-bold small d-block">${escapeHtml(participant.challengeTitle)}</span>
                        <span class="badge bg-light text-dark border">${escapeHtml(participant.activityType)}</span>
                    </div>
                </td>
                <td><strong>${participant.totalDistanceKm.toFixed(1)} km</strong><br><small class="text-muted">/ ${participant.challengeGoalKm} km</small></td>
                <td>${participant.totalActivities}</td>
                <td class="fw-bold">${participant.totalTimeFormatted}</td>
                <td style="width: 120px;">
                    <div class="progress" style="height: 6px;">
                        <div class="progress-bar bg-success" style="width: ${progressPercent}%"></div>
                    </div>
                    <small class="text-muted">${progressPercent.toFixed(1)}%</small>
                </td>
            `;
            leaderboardBody.appendChild(row);
        });
        
    } catch (error) {
        console.error('Error loading leaderboard:', error);
        const leaderboardBody = document.getElementById('leaderboardBody');
        if (leaderboardBody) {
            leaderboardBody.innerHTML = '<tr><td colspan="8" class="text-center py-4 text-danger">Error loading leaderboard: ' + error.message + '</td></tr>';
        }
        showToast('Error loading leaderboard: ' + error.message, true);
    }
}

// Load challenges for filter dropdown
async function loadChallengeFilters() {
    try {
        const response = await fetch('/Admin/GetActiveChallenges');
        const challenges = await response.json();
        
        const filterSelect = document.getElementById('challengeFilter');
        if (!filterSelect) return;
        
        // Clear existing options except the "All Challenges" option
        while (filterSelect.options.length > 1) {
            filterSelect.remove(1);
        }
        
        // Add challenges to dropdown
        challenges.forEach(challenge => {
            const option = document.createElement('option');
            option.value = challenge.challengeId;
            option.textContent = `${challenge.title} (${challenge.activityType})`;
            filterSelect.appendChild(option);
        });
        
    } catch (error) {
        console.error('Error loading challenge filters:', error);
    }
}

// Filter leaderboard by challenge
function filterLeaderboardByChallenge() {
    const filterSelect = document.getElementById('challengeFilter');
    if (filterSelect) {
        selectedChallengeId = filterSelect.value;
        loadLeaderboard();
    }
}

// Filter leaderboard by search term (for athlete name)
function filterLeaderboardBySearch() {
    const searchTerm = document.getElementById('leaderboardSearch')?.value.toLowerCase() || '';
    const rows = document.querySelectorAll('#leaderboardBody tr');
    
    rows.forEach(row => {
        const athleteName = row.querySelector('td:nth-child(2) .fw-bold')?.innerText.toLowerCase() || '';
        row.style.display = athleteName.includes(searchTerm) ? '' : 'none';
    });
}

// Tab switching
// Update the switchChallengeTab function to include prizes tab
function switchChallengeTab(viewName) {
    const leaderboardView = document.getElementById('view-leaderboard');
    const tasksView = document.getElementById('view-active-tasks');
    const prizesView = document.getElementById('view-prizes');
    const btnLead = document.getElementById('tab-leaderboard');
    const btnTasks = document.getElementById('tab-active-tasks');
    const btnPrizes = document.getElementById('tab-prizes');

    // Hide all views
    if (leaderboardView) leaderboardView.classList.add('d-none');
    if (tasksView) tasksView.classList.add('d-none');
    if (prizesView) prizesView.classList.add('d-none');

    // Reset button styles
    if (btnLead) btnLead.className = "btn btn-sm text-muted rounded-0 px-3 py-2 me-1";
    if (btnTasks) btnTasks.className = "btn btn-sm text-muted rounded-0 px-3 py-2 me-1";
    if (btnPrizes) btnPrizes.className = "btn btn-sm text-muted rounded-0 px-3 py-2 me-1";

    // Show selected view and activate button
    if (viewName === 'leaderboard') {
        if (leaderboardView) leaderboardView.classList.remove('d-none');
        if (btnLead) btnLead.className = "btn btn-sm fw-bold bg-dark text-white rounded-0 px-3 py-2 active-tab me-1";
        loadLeaderboard();
    } else if (viewName === 'active-tasks') {
        if (tasksView) tasksView.classList.remove('d-none');
        if (btnTasks) btnTasks.className = "btn btn-sm fw-bold bg-dark text-white rounded-0 px-3 py-2 active-tab me-1";
        loadAllChallenges();
    } else if (viewName === 'prizes') {
        if (prizesView) prizesView.classList.remove('d-none');
        if (btnPrizes) btnPrizes.className = "btn btn-sm fw-bold bg-dark text-white rounded-0 px-3 py-2 active-tab me-1";
        loadPrizes();
    }
}

function addPrizeTier() {
    prizeTierCount++;
    const template = document.getElementById('prizeTypeTemplate');
    if (!template) return;
    
    const clone = template.cloneNode(true);
    clone.removeAttribute('id');
    clone.style.display = 'block';
    
    const tierSpan = clone.querySelector('.tier-number');
    if (tierSpan) tierSpan.textContent = prizeTierCount;
    
    const container = document.getElementById('prizeTiersContainer');
    if (container) container.appendChild(clone);
}

function removePrizeTier(button) {
    const card = button.closest('.prize-tier-card');
    if (card) card.remove();
    
    // Re-number remaining tiers
    const tiers = document.querySelectorAll('.prize-tier-card');
    tiers.forEach((tier, index) => {
        const tierSpan = tier.querySelector('.tier-number');
        if (tierSpan) tierSpan.textContent = index + 1;
    });
    prizeTierCount = tiers.length;
}

function togglePrizeFields(select) {
    const card = select.closest('.prize-tier-card');
    if (!card) return;
    
    const cashFields = card.querySelector('.cash-fields');
    const voucherFields = card.querySelector('.voucher-fields');
    const rewardFields = card.querySelector('.reward-fields');
    const prizeTypeId = card.querySelector('.prize-type-id');
    const voucherType = card.querySelector('.voucher-type');
    
    // Hide all first
    if (cashFields) cashFields.style.display = 'none';
    if (voucherFields) voucherFields.style.display = 'none';
    if (rewardFields) rewardFields.style.display = 'none';
    
    // Show based on selection
    switch(select.value) {
        case 'cash':
            if (cashFields) cashFields.style.display = 'block';
            if (prizeTypeId) prizeTypeId.value = '1';
            break;
        case 'shipping_voucher':
            if (voucherFields) voucherFields.style.display = 'block';
            if (prizeTypeId) prizeTypeId.value = '2';
            if (voucherType) voucherType.value = 'SHIPPING';
            break;
        case 'product_voucher':
            if (voucherFields) voucherFields.style.display = 'block';
            if (prizeTypeId) prizeTypeId.value = '3';
            if (voucherType) voucherType.value = 'PRODUCT';
            break;
        case 'reward':
            if (rewardFields) rewardFields.style.display = 'block';
            if (prizeTypeId) prizeTypeId.value = '4';
            break;
    }
}

function getPrizeData() {
    const prizes = [];
    const prizeCards = document.querySelectorAll('.prize-tier-card');
    
    prizeCards.forEach((card, index) => {
        const prizeTypeSelect = card.querySelector('.prize-type-select');
        const prizeType = prizeTypeSelect ? prizeTypeSelect.value : '';
        
        if (!prizeType) return;
        
        const prize = {
            tier: index + 1,
            tierName: card.querySelector('.tier-name')?.value || '',
            description: card.querySelector('.prize-description')?.value || '',
            prizeTypeId: parseInt(card.querySelector('.prize-type-id')?.value || 0),
            quantity: 1
        };
        
        if (prizeType === 'cash') {
            prize.cashAmount = parseFloat(card.querySelector('.cash-amount')?.value) || 0;
        } else if (prizeType === 'shipping_voucher' || prizeType === 'product_voucher') {
            prize.voucherDiscountPercent = parseFloat(card.querySelector('.voucher-percent')?.value) || null;
            prize.voucherDiscountFixed = parseFloat(card.querySelector('.voucher-fixed')?.value) || null;
            prize.voucherMinimumPurchase = parseFloat(card.querySelector('.voucher-min-purchase')?.value) || null;
            prize.voucherType = card.querySelector('.voucher-type')?.value || '';
        } else if (prizeType === 'reward') {
            prize.rewardName = card.querySelector('.reward-name')?.value || '';
            prize.rewardValue = parseFloat(card.querySelector('.reward-value')?.value) || 0;
        }
        
        prizes.push(prize);
    });
    
    return prizes;
}

// Handle challenge submit
async function handleChallengeSubmit(event) {
    event.preventDefault();
    
    const title = document.getElementById('challengeTitle')?.value;
    const description = document.getElementById('challengeDesc')?.value;
    const rules = document.getElementById('challengeRules')?.value;
    const prizes = document.getElementById('challengePrizes')?.value;
    const goalKm = parseFloat(document.getElementById('goalKm')?.value);
    const activityType = document.getElementById('activityType')?.value;
    const startDate = document.getElementById('startDate')?.value;
    const endDate = document.getElementById('endDate')?.value;
    const status = document.getElementById('challengeStatus')?.value;
    const hasPrizesRadio = document.querySelector('input[name="hasPrizes"]:checked');
    const hasPrizes = hasPrizesRadio ? hasPrizesRadio.value : 'no';
    let bannerBase64 = null;
    let bannerImageName = null;
    let bannerImageContentType = null;
    
    const fileInput = document.getElementById('challengeImgInput');
    
    // Validate required fields
    if (!title || !goalKm || !activityType || !startDate || !endDate) {
        showToast('Please fill all required fields', true);
        return;
    }
    
    // Validate goalKm
    if (isNaN(goalKm) || goalKm <= 0) {
        showToast('Please enter a valid goal distance (greater than 0)', true);
        return;
    }
    
    // Get prize data if has prizes
    let prizeData = [];
    if (hasPrizes === 'yes') {
        prizeData = getPrizeData();
        if (prizeData.length === 0) {
            showToast('Please add at least one prize tier', true);
            return;
        }
    }
    
    // Date validation
    const currentDate = new Date();
    currentDate.setHours(0, 0, 0, 0); 
    
    const start = new Date(startDate);
    start.setHours(0, 0, 0, 0);
    
    const end = new Date(endDate);
    end.setHours(0, 0, 0, 0);
    
    if (start < currentDate) {
        showToast('Start date cannot be before current date', true);
        return;
    }
    
    if (end < start) {
        showToast('End date cannot be before start date', true);
        return;
    }
    
    if (end < currentDate) {
        showToast('End date cannot be before current date', true);
        return;
    }
    
    // Process image
    if (fileInput && fileInput.files && fileInput.files.length > 0) {
        const file = fileInput.files[0];
        
        if (!file.type.startsWith('image/')) {
            showToast('Please select an image file', true);
            return;
        }
        
        if (file.size > 5 * 1024 * 1024) {
            showToast('Image size must be less than 5MB', true);
            return;
        }
        
        try {
            bannerImageName = file.name;
            bannerImageContentType = file.type;
            
            const base64 = await new Promise((resolve, reject) => {
                const reader = new FileReader();
                reader.onload = () => resolve(reader.result);
                reader.onerror = reject;
                reader.readAsDataURL(file);
            });
            
            bannerBase64 = base64;
        } catch (error) {
            console.error('Error reading file:', error);
            showToast('Error reading image file', true);
            return;
        }
    }
    
    const confirmModal = new bootstrap.Modal(document.getElementById('confirmLaunchModal'));
    const confirmTitle = document.getElementById('confirmTitle');
    const confirmMessage = document.getElementById('confirmMessage');
    
    if (confirmTitle) confirmTitle.innerText = isEditMode ? 'Save Changes?' : 'Launch Challenge?';
    if (confirmMessage) confirmMessage.innerText = isEditMode ? 
        'Updates will be visible to all participants immediately.' : 
        'This will notify all athletes and make the challenge live.';
    
    const confirmBtn = document.getElementById('confirmExecuteBtn');
    if (confirmBtn) {
        confirmBtn.onclick = async () => {
            confirmModal.hide();
            
            const url = isEditMode ? '/Admin/UpdateChallenge' : '/Admin/CreateChallenge';
            const body = {
                challengeId: currentChallengeId,
                title: title,
                description: description,
                rules: rules,
                prizes: prizes,
                goalKm: goalKm,
                activityType: activityType,
                startDate: startDate,
                endDate: endDate,
                status: status,
                bannerBase64: bannerBase64,  
                bannerImageName: bannerImageName,
                bannerImageContentType: bannerImageContentType,
                prizesData: prizeData
            };
            
            if (!isEditMode) delete body.challengeId;
            
            try {
                const response = await fetch(url, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(body)
                });
                
                const data = await response.json();
                
                if (data.success) {
                    showToast(data.message);
                    const modal = bootstrap.Modal.getInstance(document.getElementById('launchChallengeModal'));
                    if (modal) modal.hide();
                    
                    if (isEditMode && window.location.pathname.includes('ChallengeDetails')) {
                        window.location.reload();
                    } else {
                        loadAllChallenges();
                        loadLeaderboard();
                        loadChallengeStatistics();
                    }
                    
                    // Reset form
                    const form = document.getElementById('launchForm');
                    if (form) form.reset();
                    
                    const preview = document.getElementById('imagePreview');
                    const placeholder = document.getElementById('uploadPlaceholder');
                    if (preview) {
                        preview.classList.add('d-none');
                        preview.src = '#';
                    }
                    if (placeholder) placeholder.classList.remove('d-none');
                    if (fileInput) fileInput.value = '';
                    
                    // Reset prize section
                    const container = document.getElementById('prizeTiersContainer');
                    if (container) container.innerHTML = '';
                    prizeTierCount = 0;
                    const hasPrizesYes = document.getElementById('hasPrizesYes');
                    if (hasPrizesYes) hasPrizesYes.checked = false;
                    togglePrizeManagement('no');
                } else {
                    showToast(data.message || 'Error saving challenge', true);
                }
            } catch (error) {
                console.error('Error saving challenge:', error);
                showToast('Error saving challenge', true);
            }
        };
    }
    
    confirmModal.show();
}

function togglePrizeManagement(hasPrizes) {
    const prizeSection = document.getElementById('prizeManagementSection');
    if (hasPrizes === 'yes') {
        if (prizeSection) prizeSection.style.display = 'block';
    } else {
        if (prizeSection) prizeSection.style.display = 'none';
        // Clear existing prize tiers when switching to "No prizes"
        const container = document.getElementById('prizeTiersContainer');
        if (container) container.innerHTML = '';
        prizeTierCount = 0;
    }
}

// Open user view modal
function openUserView(name, rank, distance, activities, time, avatarUrl) {
    const userNameView = document.getElementById('userNameView');
    const userRankView = document.getElementById('userRankView');
    const userDistView = document.getElementById('userDistView');
    const userActView = document.getElementById('userActView');
    const userTimeView = document.getElementById('userTimeView');
    const userModalPic = document.getElementById('userModalPic');
    
    if (userNameView) userNameView.innerText = '@' + name;
    if (userRankView) userRankView.innerText = 'Ranked #' + rank + ' Global';
    if (userDistView) userDistView.innerText = (distance || 0).toFixed(1) + ' km';
    if (userActView) userActView.innerText = activities || 0;
    if (userTimeView) userTimeView.innerText = time || '00:00:00';
    if (userModalPic) userModalPic.src = avatarUrl || `https://ui-avatars.com/api/?name=${encodeURIComponent(name)}&background=random`;
    
    const modal = document.getElementById('userViewModal');
    if (modal) new bootstrap.Modal(modal).show();
}

// Helper functions
function formatDate(dateString) {
    if (!dateString) return 'N/A';
    const date = new Date(dateString);
    return date.toLocaleDateString('en-PH', { month: 'short', day: 'numeric', year: 'numeric' });
}

function formatTime(seconds) {
    if (!seconds || seconds <= 0) return '00:00:00';
    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    const secs = seconds % 60;
    return `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
}

function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

function showToast(message, isError = false) {
    const toastMsg = document.getElementById('toastMsg');
    const toastIcon = document.getElementById('toastIcon');
    const toastEl = document.getElementById('challengeToast');
    
    if (!toastMsg || !toastEl) {
        if (isError) alert('Error: ' + message);
        else console.log('Toast: ' + message);
        return;
    }
    
    toastMsg.innerText = message;
    toastIcon.className = isError ? 'bi bi-exclamation-triangle-fill text-danger fs-5' : 'bi bi-check-circle-fill text-success fs-5';
    
    const toast = new bootstrap.Toast(toastEl, { delay: 3000 });
    toast.show();
}