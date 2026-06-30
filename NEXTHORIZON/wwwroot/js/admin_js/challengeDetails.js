﻿// Get challenge ID from URL
const challengeId = window.location.pathname.split('/').pop();

// Load challenge details on page load
document.addEventListener('DOMContentLoaded', function() {
    loadChallengeDetails();
    setupSearch();
    loadUserPrizes();
});

async function loadChallengeDetails() {
    try {
        const response = await fetch(`/Admin/GetChallengeDetails?id=${challengeId}`);
        const data = await response.json();
        
        if (!data.success) {
            showToast('Error loading challenge details', true);
            return;
        }
        
        const challenge = data.challenge;
        const leaderboard = data.leaderboard;
        
        if (!challenge) {
            showToast('Challenge data not found', true);
            return;
        }
        
        // Update header
        const titleBreadcrumb = document.getElementById('challengeTitleBreadcrumb');
        const challengeTitle = document.getElementById('challengeTitle2');
        const challengeBanner = document.getElementById('challengeBanner');
        
        if (titleBreadcrumb) titleBreadcrumb.innerText = challenge.title || 'Challenge';
        if (challengeTitle) challengeTitle.innerText = challenge.title || 'Untitled Challenge';
        
        // Handle banner image
        if (challengeBanner) {
            challengeBanner.src = challenge.bannerBase64 || '/images/challenge-placeholder.jpg';
            challengeBanner.onerror = function() {
                this.src = '/images/challenge-placeholder.jpg';
            };
        }
        
        // Status badge
        const statusBadge = document.getElementById('challengeStatusBadge');
        if (statusBadge) {
            statusBadge.innerText = challenge.status || 'Unknown';
            if (challenge.status === 'Live') statusBadge.className = 'badge bg-success rounded-pill';
            else if (challenge.status === 'Upcoming') statusBadge.className = 'badge bg-warning rounded-pill';
            else if (challenge.status === 'Completed') statusBadge.className = 'badge bg-secondary rounded-pill';
            else statusBadge.className = 'badge bg-danger rounded-pill';
        }
        
        // Challenge dates
        const challengeDates = document.getElementById('challengeDates');
        if (challengeDates) {
            challengeDates.innerText = challenge.startDate && challenge.endDate ? 
                `${formatDate(challenge.startDate)} - ${formatDate(challenge.endDate)}` : 
                'Date range not set';
        }
        
        // Stats
        const totalParticipants = document.getElementById('totalParticipants');
        const totalCompleted = document.getElementById('totalCompleted');
        const avgDistance = document.getElementById('avgDistance');
        const avgTime = document.getElementById('avgTime');
        
        if (totalParticipants) totalParticipants.innerText = challenge.totalParticipants || 0;
        if (totalCompleted) totalCompleted.innerText = challenge.totalCompleted || 0;
        if (avgDistance) avgDistance.innerText = (data.avgDistanceKm || 0).toFixed(1) + ' km';
        if (avgTime) avgTime.innerText = (data.avgTimeMinutes || 0).toFixed(0) + ' min';
        
        // Leaderboard
        const tbody = document.getElementById('leaderboardBody');
        if (!tbody) return;
        
        if (!leaderboard || leaderboard.length === 0) {
            tbody.innerHTML = '<td colspan="7" class="text-center py-4">No participants yet</td>';
            return;
        }
        
        tbody.innerHTML = '';
        leaderboard.forEach(participant => {
            const progressPercent = (participant.totalDistanceKm / challenge.goalKm * 100).toFixed(1);
            const row = document.createElement('tr');
            row.innerHTML = `
                <td class="fw-bold">#${participant.rank}</td>
                <td class="text-start">
                    <div class="d-flex align-items-center gap-2">
                        <img src="${participant.avatarUrl || 'https://ui-avatars.com/api/?name=' + encodeURIComponent(participant.athleteName)}" class="rounded-circle" width="32" height="32" onerror="this.src='https://ui-avatars.com/api/?name=${encodeURIComponent(participant.athleteName)}'">
                        <div>
                            <div class="fw-bold mb-0">${escapeHtml(participant.athleteName)}</div>
                            <small class="text-muted">${escapeHtml(participant.email || participant.username || '')}</small>
                        </div>
                    </div>
                </td>
                <td style="width: 180px;">
                    <div class="d-flex align-items-center gap-2">
                        <div class="progress flex-grow-1" style="height: 6px;">
                            <div class="progress-bar bg-success" style="width: ${progressPercent}%"></div>
                        </div>
                        <small class="fw-bold">${progressPercent}%</small>
                    </div>
                </td>
                <td>${participant.totalDistanceKm.toFixed(1)} km</td>
                <td>${participant.totalActivities}</td>
                <td>${participant.totalTimeFormatted}</td>
                <td>
                    <button class="btn btn-sm btn-light border rounded-pill px-3"
                            onclick="viewActivityLog(${participant.participantId}, '${escapeHtml(participant.athleteName)}')">
                        View Logs
                    </button>
                </td>
            `;
            tbody.appendChild(row);
        });
        
    } catch (error) {
        console.error('Error loading challenge details:', error);
        showToast('Error loading challenge details: ' + error.message, true);
    }
}

// Load user's prizes for this challenge
async function loadUserPrizes() {
    try {
        const userId = getCurrentUserId();
        console.log('Loading prizes for challenge:', challengeId, 'user:', userId);
        
        const response = await fetch(`/Admin/GetUserPrizes?challengeId=${challengeId}&userId=${userId}`);
        console.log('Response status:', response.status);
        
        const data = await response.json();
        console.log('Prizes data received:', data);
        
        const prizeSection = document.getElementById('prizeSection');
        const prizeListContainer = document.getElementById('prizeList');
        const noPrizeMessage = document.getElementById('noPrizeMessage');
        
        if (!prizeListContainer || !noPrizeMessage) {
            console.error('Prize containers not found');
            return;
        }
        
        // Check if this is Admin view
        const isAdminView = data.isAdminView === true;
        
        if (!data.success) {
            console.error('Error from server:', data.error);
            prizeListContainer.style.display = 'none';
            noPrizeMessage.style.display = 'block';
            noPrizeMessage.innerHTML = '<p class="text-muted mt-2">Error loading prizes</p>';
            return;
        }
        
        if (!data.prizes || data.prizes.length === 0) {
            console.log('No prizes found');
            prizeListContainer.style.display = 'none';
            noPrizeMessage.style.display = 'block';
            
            if (isAdminView) {
                noPrizeMessage.innerHTML = '<i class="bi bi-trophy text-muted fs-1"></i><p class="text-muted mt-2">No prizes configured for this challenge yet.<br>You can add prizes when editing the challenge.</p>';
            } else {
                noPrizeMessage.innerHTML = '<i class="bi bi-trophy text-muted fs-1"></i><p class="text-muted mt-2">No prizes assigned for this challenge yet</p>';
            }
            return;
        }
        
        // Show prize section
        if (prizeSection) prizeSection.style.display = 'block';
        noPrizeMessage.style.display = 'none';
        prizeListContainer.style.display = 'block';
        prizeListContainer.innerHTML = '';
        
        // Update header based on view type
        const prizeHeader = document.querySelector('#prizeSection .card-header h5');
        if (prizeHeader) {
            if (isAdminView) {
                prizeHeader.innerHTML = '<i class="bi bi-trophy me-2 text-warning"></i>Challenge Prizes';
            } else {
                prizeHeader.innerHTML = '<i class="bi bi-gift text-warning me-2"></i>Your Prizes';
            }
        }
        
        data.prizes.forEach(prize => {
            let prizeIcon = '';
            let prizeDetails = '';
            let claimButton = '';
            let adminInfo = '';
            let statusBadge = '';
            
            // Determine prize icon and details based on type
            switch(prize.prizeType) {
                case 'Cash':
                    prizeIcon = '💰';
                    prizeDetails = `₱${(prize.cashAmount || 0).toLocaleString()} Cash Prize`;
                    break;
                case 'Shipping Voucher':
                    prizeIcon = '📦';
                    prizeDetails = `Free Shipping Voucher${prize.voucherDiscountPercent ? ` (${prize.voucherDiscountPercent}% off)` : ''}`;
                    break;
                case 'Product Discount':
                    prizeIcon = '🏷️';
                    prizeDetails = `${prize.voucherDiscountPercent || 0}% off Product Discount${prize.voucherMinimumPurchase ? ` (min. ₱${prize.voucherMinimumPurchase})` : ''}`;
                    break;
                case 'Physical Reward':
                    prizeIcon = '🎁';
                    prizeDetails = `${prize.rewardName || 'Physical Reward'} (Value: ₱${prize.rewardValue || 0})`;
                    break;
                default:
                    prizeIcon = '🏆';
                    prizeDetails = prize.description || 'Prize';
            }
            
            // Admin view - show assignment status
            if (isAdminView) {
                const assignmentStatus = prize.assignmentStatus || 'Unassigned';
                const athleteName = prize.athleteName || 'Not yet assigned';
                
                if (assignmentStatus === 'Assigned') {
                    statusBadge = `<span class="badge bg-success ms-2">Assigned</span>`;
                    adminInfo = `
                        <div class="small text-muted mt-1">
                            <i class="bi bi-person"></i> Winner: ${escapeHtml(athleteName)}
                        </div>
                        ${prize.claimCode ? `<div class="small text-muted"><i class="bi bi-qr-code"></i> Code: <code>${escapeHtml(prize.claimCode)}</code></div>` : ''}
                        ${prize.claimStatus ? `<div class="small text-muted"><i class="bi bi-info-circle"></i> Status: ${prize.claimStatus}</div>` : ''}
                    `;
                } else {
                    statusBadge = `<span class="badge bg-warning text-dark ms-2">Unassigned</span>`;
                    adminInfo = `
                        <div class="small text-muted mt-1">
                            <i class="bi bi-hourglass"></i> Not yet assigned to any participant
                        </div>
                    `;
                }
            }
            
            // User view - show claim button if eligible
            if (!isAdminView && prize.claimStatus === 'Eligible') {
                claimButton = `<button class="btn btn-sm btn-success rounded-pill" onclick="openClaimForm(${prize.winnerId}, '${prize.prizeType}', ${prize.cashAmount || 0})">
                    <i class="bi bi-gift me-1"></i>Claim Prize
                </button>`;
            } else if (!isAdminView && prize.claimStatus === 'Processing') {
                claimButton = `<span class="badge bg-warning text-dark">Claim Submitted - Pending Approval</span>`;
            } else if (!isAdminView && prize.claimStatus === 'Completed') {
                claimButton = `<span class="badge bg-success">Prize Claimed ✓</span>`;
            } else if (!isAdminView && prize.claimStatus === 'Rejected') {
                claimButton = `<span class="badge bg-danger">Claim Rejected</span>`;
            } else if (!isAdminView && prize.claimStatus === 'Expired') {
                claimButton = `<span class="badge bg-secondary">Claim Expired</span>`;
            }
            
            const prizeCard = document.createElement('div');
            prizeCard.className = 'border rounded-3 p-3 mb-3';
            prizeCard.innerHTML = `
                <div class="d-flex justify-content-between align-items-start">
                    <div class="flex-grow-1">
                        <div class="d-flex align-items-center gap-2 mb-1">
                            <span class="fs-4">${prizeIcon}</span>
                            <h6 class="fw-bold mb-0">${escapeHtml(prize.tierName || `#${prize.tier} Place`)}</h6>
                            ${statusBadge}
                        </div>
                        <p class="small text-muted mb-1">${escapeHtml(prize.description)}</p>
                        <p class="small fw-bold text-primary mb-0">${prizeDetails}</p>
                        ${!isAdminView && prize.claimDeadline ? `<small class="text-muted">Claim by: ${new Date(prize.claimDeadline).toLocaleDateString()}</small>` : ''}
                        ${adminInfo}
                    </div>
                    <div class="ms-3">${claimButton}</div>
                </div>
                ${!isAdminView && prize.claimCode ? `
                <div class="mt-2">
                    <small class="text-muted">Claim Code: <code class="bg-light p-1 rounded">${escapeHtml(prize.claimCode)}</code></small>
                </div>
                ` : ''}
            `;
            prizeListContainer.appendChild(prizeCard);
        });
    } catch (error) {
        console.error('Error loading prizes:', error);
        const prizeListContainer = document.getElementById('prizeList');
        const noPrizeMessage = document.getElementById('noPrizeMessage');
        if (prizeListContainer) prizeListContainer.style.display = 'none';
        if (noPrizeMessage) {
            noPrizeMessage.style.display = 'block';
            noPrizeMessage.innerHTML = '<p class="text-danger mt-2">Error loading prizes. Please try again.</p>';
        }
    }
}

// Open claim form based on prize type
function openClaimForm(winnerId, prizeType, cashAmount) {
    const modal = new bootstrap.Modal(document.getElementById('claimPrizeModal'));
    const formContainer = document.getElementById('claimPrizeForm');
    
    if (!formContainer) return;
    
    let formHtml = '';
    
    if (prizeType === 'Cash') {
        formHtml = `
            <h6 class="fw-bold mb-3">Cash Prize Claim</h6>
            <p class="text-muted small">Amount: ₱${cashAmount.toLocaleString()}</p>
            <div class="mb-3">
                <label class="form-label fw-bold">Bank Name</label>
                <input type="text" class="form-control" id="bankName" required>
            </div>
            <div class="mb-3">
                <label class="form-label fw-bold">Account Number</label>
                <input type="text" class="form-control" id="accountNumber" required>
            </div>
            <div class="mb-3">
                <label class="form-label fw-bold">Account Name</label>
                <input type="text" class="form-control" id="accountName" required>
            </div>
            <div class="mb-3">
                <label class="form-label fw-bold">Notes (Optional)</label>
                <textarea class="form-control" id="claimNotes" rows="2"></textarea>
            </div>
        `;
    } else if (prizeType === 'Physical Reward') {
        formHtml = `
            <h6 class="fw-bold mb-3">Physical Reward Claim</h6>
            <div class="mb-3">
                <label class="form-label fw-bold">Shipping Address</label>
                <textarea class="form-control" id="shippingAddress" rows="3" required></textarea>
            </div>
            <div class="mb-3">
                <label class="form-label fw-bold">Notes (Optional)</label>
                <textarea class="form-control" id="claimNotes" rows="2"></textarea>
            </div>
        `;
    } else {
        formHtml = `
            <h6 class="fw-bold mb-3">Voucher Prize Claim</h6>
            <p class="text-muted">You will receive a unique voucher code upon approval.</p>
            <div class="mb-3">
                <label class="form-label fw-bold">Notes (Optional)</label>
                <textarea class="form-control" id="claimNotes" rows="2"></textarea>
            </div>
        `;
    }
    
    formHtml += `
        <div class="d-grid gap-2 mt-4">
            <button class="btn btn-primary rounded-pill py-2" onclick="submitClaim(${winnerId})">
                <i class="bi bi-check-circle me-2"></i>Submit Claim
            </button>
            <button class="btn btn-light rounded-pill" data-bs-dismiss="modal">Cancel</button>
        </div>
    `;
    
    formContainer.innerHTML = formHtml;
    modal.show();
}

// Submit prize claim
async function submitClaim(winnerId) {
    const claimDetails = {};
    
    const bankName = document.getElementById('bankName')?.value;
    const accountNumber = document.getElementById('accountNumber')?.value;
    const accountName = document.getElementById('accountName')?.value;
    const shippingAddress = document.getElementById('shippingAddress')?.value;
    const notes = document.getElementById('claimNotes')?.value;
    
    if (bankName) claimDetails.bank_name = bankName;
    if (accountNumber) claimDetails.account_number = accountNumber;
    if (accountName) claimDetails.account_name = accountName;
    if (shippingAddress) claimDetails.shipping_address = shippingAddress;
    if (notes) claimDetails.notes = notes;
    
    try {
        const response = await fetch('/Admin/ClaimPrize', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                winnerId: winnerId,
                claimDetails: JSON.stringify(claimDetails)
            })
        });
        
        const data = await response.json();
        
        if (data.success) {
            showToast(data.message);
            bootstrap.Modal.getInstance(document.getElementById('claimPrizeModal')).hide();
            loadUserPrizes();
        } else {
            showToast(data.message, true);
        }
    } catch (error) {
        console.error('Error submitting claim:', error);
        showToast('Error submitting claim', true);
    }
}

// Verify and claim prize with code
async function verifyAndClaimPrize() {
    const claimCode = document.getElementById('claimCodeInput')?.value.trim();
    
    if (!claimCode) {
        showToast('Please enter your claim code', true);
        return;
    }
    
    try {
        const response = await fetch('/Admin/VerifyAndClaimPrize', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ claimCode: claimCode })
        });
        
        const data = await response.json();
        
        if (data.success) {
            showToast(data.message);
            bootstrap.Modal.getInstance(document.getElementById('verifyPrizeCodeModal')).hide();
            loadUserPrizes();
        } else {
            showToast(data.message, true);
        }
    } catch (error) {
        console.error('Error verifying prize:', error);
        showToast('Error verifying prize code', true);
    }
}

// Open verify prize modal
function openVerifyPrizeModal() {
    const modal = new bootstrap.Modal(document.getElementById('verifyPrizeCodeModal'));
    modal.show();
}

async function viewActivityLog(participantId, athleteName) {
    try {
        const response = await fetch(`/Admin/GetParticipantActivities?participantId=${participantId}`);
        const data = await response.json();
        
        if (!data.success) {
            showToast('Error loading activities', true);
            return;
        }
        
        const detailAthleteName = document.getElementById('detailAthleteName');
        if (detailAthleteName) detailAthleteName.innerText = athleteName;
        
        const container = document.getElementById('activityLogList');
        if (!container) return;
        
        if (!data.activities || data.activities.length === 0) {
            container.innerHTML = '<div class="text-center py-4">No activities logged</div>';
        } else {
            container.innerHTML = '';
            data.activities.forEach(activity => {
                const activityDate = new Date(activity.activityDate);
                const formattedDate = activityDate.toLocaleDateString('en-PH', { 
                    month: 'short', 
                    day: 'numeric', 
                    hour: '2-digit', 
                    minute: '2-digit' 
                });
                
                const activityDiv = document.createElement('div');
                activityDiv.className = 'list-group-item p-3 border-0 border-bottom text-start';
                activityDiv.innerHTML = `
                    <div class="d-flex justify-content-between align-items-center mb-2">
                        <div>
                            <span class="fw-bold small text-dark">${escapeHtml(activity.activityType)}</span>
                            ${activity.challengeTitle ? `<span class="badge bg-light text-dark ms-2">${escapeHtml(activity.challengeTitle)}</span>` : ''}
                        </div>
                        <span class="badge bg-primary">${activity.distanceKm.toFixed(2)} km</span>
                    </div>
                    <div class="d-flex justify-content-between small text-muted mb-2">
                        <span><i class="bi bi-calendar me-1"></i>${formattedDate}</span>
                        <span><i class="bi bi-stopwatch me-1"></i>${activity.durationFormatted || formatTime(activity.durationSeconds)}</span>
                        <span><i class="bi bi-speedometer2 me-1"></i>${activity.averagePace.toFixed(2)} min/km</span>
                    </div>
                    <div class="d-flex justify-content-between align-items-center">
                        <span class="${activity.isVerified ? 'text-success' : 'text-warning'} small">
                            <i class="bi ${activity.isVerified ? 'bi-check-circle-fill' : 'bi-clock-history'} me-1"></i>
                            ${activity.isVerified ? `Verified${activity.verifiedByName ? ' by ' + escapeHtml(activity.verifiedByName) : ''}` : 'Pending Verification'}
                        </span>
                        ${!activity.isVerified ? `<button class="btn btn-sm btn-outline-success rounded-pill" onclick="verifyActivity(${activity.activityId})">Verify Activity</button>` : ''}
                    </div>
                    ${activity.notes ? `<div class="small text-muted mt-2 border-top pt-2"><i class="bi bi-chat me-1"></i>${escapeHtml(activity.notes)}</div>` : ''}
                    ${activity.imageProofBase64 ? `
                        <div class="mt-2">
                            <button class="btn btn-sm btn-link p-0 text-primary" onclick="viewActivityProof('${activity.imageProofBase64}')">
                                <i class="bi bi-image"></i> View Proof Image
                            </button>
                        </div>
                    ` : ''}
                `;
                container.appendChild(activityDiv);
            });
        }
        
        const modal = document.getElementById('activityDetailsModal');
        if (modal) new bootstrap.Modal(modal).show();
    } catch (error) {
        console.error('Error loading activities:', error);
        showToast('Error loading activities', true);
    }
}

// Add this function to view the proof image
function viewActivityProof(base64Image) {
    const modal = new bootstrap.Modal(document.getElementById('imageProofModal'));
    const imgElement = document.getElementById('proofImage');
    if (imgElement) {
        imgElement.src = `data:image/jpeg;base64,${base64Image}`;
    }
    modal.show();
}

async function verifyActivity(activityId) {
    try {
        const response = await fetch('/Admin/VerifyActivity', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ activityId: activityId })
        });
        
        const data = await response.json();
        
        if (data.success) {
            showToast('Activity verified successfully');
            const modal = bootstrap.Modal.getInstance(document.getElementById('activityDetailsModal'));
            if (modal) modal.hide();
            loadChallengeDetails();
        } else {
            showToast(data.message || 'Error verifying activity', true);
        }
    } catch (error) {
        showToast('Error verifying activity', true);
    }
}

function openEditChallengeModalFromDetails() {
    fetch(`/Admin/GetChallengeDetails?id=${challengeId}`)
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                const challenge = data.challenge;
                openEditChallengeModal(
                    challenge.challengeId,
                    challenge.title,
                    challenge.description,
                    challenge.rules,
                    challenge.prizes,
                    challenge.goalKm,
                    challenge.activityType,
                    challenge.startDate,
                    challenge.endDate,
                    challenge.status,
                    challenge.bannerBase64,
                    challenge.bannerImageName,
                    challenge.bannerImageContentType
                );
            } else {
                showToast('Failed to load challenge details', true);
            }
        })
        .catch(error => {
            console.error('Error fetching challenge details:', error);
            showToast('Error loading challenge details', true);
        });
}

function exportParticipants() {
    const table = document.querySelector(".table");
    if (!table) return;
    
    const rows = table.querySelectorAll("tr");
    let csvContent = "";
    
    rows.forEach((row) => {
        const cells = row.querySelectorAll("th, td");
        let rowData = [];
        
        cells.forEach((cell, cellIndex) => {
            if (cellIndex === cells.length - 1) return;
            let data = cell.innerText.replace(/(\r\n|\n|\r)/gm, " ").trim();
            data = data.replace(/"/g, '""');
            if (data.includes(",")) data = `"${data}"`;
            rowData.push(data);
        });
        
        csvContent += rowData.join(",") + "\n";
    });
    
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    
    const challengeTitle = document.getElementById('challengeTitle2')?.innerText.replace(/\s+/g, '_') || 'challenge';
    const date = new Date().toISOString().split('T')[0];
    
    link.setAttribute("href", url);
    link.setAttribute("download", `${challengeTitle}_Stats_${date}.csv`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
}

function setupSearch() {
    const searchInput = document.getElementById('leaderboardSearch');
    if (searchInput) {
        searchInput.addEventListener('keyup', function(e) {
            const term = e.target.value.toLowerCase();
            const rows = document.querySelectorAll("#leaderboardBody tr");
            rows.forEach(row => {
                const athleteName = row.querySelector('td:nth-child(2) .fw-bold')?.innerText.toLowerCase() || "";
                row.style.display = athleteName.includes(term) ? "" : "none";
            });
        });
    }
}

function formatDate(dateString) {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-PH', { month: 'short', day: 'numeric', year: 'numeric' });
}

function formatDateTime(dateString) {
    const date = new Date(dateString);
    return date.toLocaleString('en-PH', { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' });
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

// Placeholder for getting current user ID - implement based on your auth system
function getCurrentUserId() {
    // This should be implemented based on your authentication system
    // For now, return a test ID or get from session
    return 1; // Replace with actual user ID from session
}