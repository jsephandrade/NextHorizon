
        let activeStatusFilter = 'all';
        let selectedOrderId = '';
        let selectedCourier = '';
        let selectedDeclineOrderId = '';
        let selectedAcceptSummary = {};
        let selectedNoteOrderId = '';
        let selectedNoteCustomer = '';
        let selectedReviewOrder = {};

        function applyOrderFilters() {
            const searchValue = (document.getElementById('orderSearch')?.value || '').toLowerCase().trim();
            const categoryValue = document.getElementById('categoryFilter')?.value || 'all';
            const dateFrom = document.getElementById('dateFrom')?.value || '';
            const dateTo = document.getElementById('dateTo')?.value || '';

            document.querySelectorAll('.order-row').forEach(function (row) {
                const status = row.dataset.status || '';
                const rowDate = row.dataset.date || '';
                const rowCategory = row.dataset.category || '';
                const rowText = row.innerText.toLowerCase();

                const statusMatch = activeStatusFilter === 'all' || status === activeStatusFilter;
                const categoryMatch = categoryValue === 'all' || rowCategory === categoryValue;
                const searchMatch = !searchValue || rowText.includes(searchValue);
                const fromMatch = !dateFrom || rowDate >= dateFrom;
                const toMatch = !dateTo || rowDate <= dateTo;

                row.style.display = (statusMatch && categoryMatch && searchMatch && fromMatch && toMatch) ? '' : 'none';
            });
        }

        function toggleOrderMenu(button) {
            const wrap = button.closest('.action-menu-wrap');
            if (!wrap) return;

            const menu = wrap.querySelector('.action-menu');
            const isOpen = menu.classList.contains('open');

            document.querySelectorAll('.action-menu.open').forEach(m => m.classList.remove('open'));
            if (!isOpen) {
                menu.classList.add('open');
            }
        }

        function resetCourierSelection() {
            selectedCourier = '';
            document.querySelectorAll('.courier-option').forEach(function (option) {
                option.classList.remove('active');
            });

            const confirmBtn = document.getElementById('courierSelectionConfirmBtn');
            if (confirmBtn) {
                confirmBtn.disabled = true;
            }
        }

        function openCourierSelectionModal() {
            const modal = document.getElementById('courierSelectionModal');
            const orderText = document.getElementById('courierSelectionOrderIdText');
            if (orderText) {
                orderText.textContent = selectedOrderId;
            }
            resetCourierSelection();
            modal?.classList.add('active');
        }

        function closeCourierSelectionModal(resetSelection = true) {
            const modal = document.getElementById('courierSelectionModal');
            modal?.classList.remove('active');
            if (resetSelection) {
                resetCourierSelection();
            }
        }

        function openShipmentSummaryModal() {
            const modal = document.getElementById('shipmentSummaryModal');
            const now = new Date();
            const compactOrderId = (selectedOrderId || '').replace(/\D/g, '').slice(-12) || '000000000000';
            const trackingNum = compactOrderId.padEnd(12, '0');
            const sortCode = selectedCourier.includes('LBC') ? '320-LBC 00' :
                selectedCourier.includes('Ninja') ? '320-NVN 00' : '320-JNT 00';
            const serviceCode = selectedCourier.includes('LBC') ? 'PH-LM-LBC-notcod' :
                selectedCourier.includes('Ninja') ? 'PH-LM-NV-notcod' : 'PH-LM-JT-notcod';
            const payment = (selectedAcceptSummary.payment || '').toUpperCase();

            document.getElementById('waybillTrackingNum').textContent = trackingNum;
            document.getElementById('waybillSortingCode').textContent = sortCode;
            document.getElementById('waybillCustomerName').textContent = selectedAcceptSummary.customer || '-';
            document.getElementById('waybillContact').textContent = selectedAcceptSummary.contact || '-';
            document.getElementById('waybillAddress').innerHTML = (selectedAcceptSummary.address || '-').replace(/, /g, ',<br>');
            document.getElementById('waybillRouteCode').textContent = (compactOrderId.slice(-3) || '047').padStart(3, '0');
            document.getElementById('waybillSellerName').textContent = '@Model.SellerName'.toUpperCase();
            document.getElementById('waybillServiceCode').textContent = serviceCode;
            document.getElementById('waybillDeliveryCode').textContent = 'SC-TT-DG';
            document.getElementById('waybillPaymentMethod').textContent = payment === 'COD' ? 'COD' : 'CASHLESS';
            document.getElementById('waybillSkuId').textContent = 'SKU-' + compactOrderId;
            document.getElementById('waybillProductName').textContent = selectedAcceptSummary.product || '-';
            document.getElementById('waybillQuantity').textContent = selectedAcceptSummary.quantity || '-';
            document.getElementById('waybillOrderId').textContent = selectedOrderId || '-';
            document.getElementById('waybillPrintTime').textContent = now.getFullYear() + '-' +
                String(now.getMonth() + 1).padStart(2, '0') + '-' +
                String(now.getDate()).padStart(2, '0') + ' ' +
                String(now.getHours()).padStart(2, '0') + ':' +
                String(now.getMinutes()).padStart(2, '0');
            document.getElementById('waybillCode').textContent = 'P' + (compactOrderId.slice(-3) || '003');

            modal?.classList.add('active');
        }

        function closeShipmentSummaryModal() {
            const modal = document.getElementById('shipmentSummaryModal');
            modal?.classList.remove('active');
        }

        function printShipmentSummary() {
            const source = document.getElementById('shipmentSummaryPrintContent');
            if (!source) return;

            const printWindow = window.open('', '_blank', 'width=900,height=700');
            if (!printWindow) return;

            printWindow.document.write(
                '<!DOCTYPE html><html><head><title>Shipment Summary</title>' +
                '<style>@@page{size:8.5in 11in;margin:0}body{font-family:Arial,sans-serif;margin:0;color:#111}'+
                '.print-quarter{width:4.25in;height:5.5in;padding:.2in;box-sizing:border-box;overflow:hidden}'+
                '.jt-waybill{border:2px solid #111;border-radius:10px;overflow:hidden;font-size:11px}'+
                '.jt-waybill-header{display:flex;justify-content:space-between;align-items:center;background:#111;color:#fff;padding:8px}'+
                '.jt-waybill-body{padding:8px}.jt-sorting-code{padding:4px 8px;border-bottom:1px dashed #bbb;font-weight:700}'+
                '.jt-receiver-section,.jt-sender-section,.jt-payment-section,.jt-items-section,.jt-waybill-footer{border:1px solid #ddd;margin-top:6px;padding:6px}'+
                '.jt-item-row,.jt-items-header{display:grid;grid-template-columns:1fr 2fr .5fr;gap:6px}'+
                '.jt-waybill-footer{display:flex;justify-content:space-between;gap:8px;font-size:10px}'+
                '.barcode-lines{display:flex;gap:1px;height:20px}.barcode-lines span{width:2px;background:#111;display:block}</style></head><body>' +
                '<div class="print-quarter">' + source.innerHTML + '</div></body></html>'
            );
            printWindow.document.close();
            printWindow.focus();
            printWindow.print();
        }

        function closeAllAcceptFlowModals() {
            closeCourierSelectionModal();
            closeShipmentSummaryModal();
            selectedOrderId = '';
            selectedCourier = '';
            selectedAcceptSummary = {};
        }

        function openAddNotesModal(orderId, customer) {
            selectedNoteOrderId = orderId || '';
            selectedNoteCustomer = customer || '';

            const overlay = document.getElementById('addNotesModal');
            const orderText = document.getElementById('addNotesOrderIdText');
            const customerText = document.getElementById('addNotesCustomerText');
            const textarea = document.getElementById('addNotesTextarea');
            const charCount = document.getElementById('addNotesCharCount');
            const error = document.getElementById('addNotesError');

            if (orderText) orderText.textContent = selectedNoteOrderId;
            if (customerText) customerText.textContent = selectedNoteCustomer || '-';
            if (textarea) textarea.value = '';
            if (charCount) charCount.textContent = '0';
            if (error) error.classList.remove('active');

            overlay?.classList.add('active');
            textarea?.focus();
            document.querySelectorAll('.action-menu.open').forEach(m => m.classList.remove('open'));
        }

        function closeAddNotesModal() {
            const overlay = document.getElementById('addNotesModal');
            overlay?.classList.remove('active');
            selectedNoteOrderId = '';
            selectedNoteCustomer = '';
        }

        function showAddNotesToast(message) {
            const toast = document.getElementById('addNotesToast');
            const text = document.getElementById('addNotesToastMessage');
            if (text) text.textContent = message;
            toast?.classList.add('active');
            window.setTimeout(function () {
                toast?.classList.remove('active');
            }, 2200);
        }

        function openReviewRequestModal(orderData) {
            selectedReviewOrder = orderData || {};
            document.getElementById('reviewOrderId').textContent = selectedReviewOrder.orderId || '-';
            document.getElementById('reviewBuyer').textContent = selectedReviewOrder.buyer || '-';
            document.getElementById('reviewProduct').textContent = selectedReviewOrder.product || '-';
            document.getElementById('reviewOrderDate').textContent = selectedReviewOrder.orderDate || '-';
            document.getElementById('reviewDeliveryDate').textContent = selectedReviewOrder.deliveryDate || '-';
            document.getElementById('reviewPaymentMethod').textContent = selectedReviewOrder.payment || '-';

            const now = new Date();
            const requestedAt = new Date(now.getTime() - 3 * 60 * 60 * 1000);
            const deadline = new Date(now.getTime() + 36 * 60 * 60 * 1000);
            const fmt = (d) => d.getFullYear() + '-' + String(d.getMonth() + 1).padStart(2, '0') + '-' + String(d.getDate()).padStart(2, '0') + ' ' + String(d.getHours()).padStart(2, '0') + ':' + String(d.getMinutes()).padStart(2, '0');

            document.getElementById('reviewRequestedAt').textContent = fmt(requestedAt);
            document.getElementById('reviewDeadline').textContent = fmt(deadline);

            document.querySelectorAll('input[name="reviewDecision"]').forEach(r => r.checked = false);
            document.getElementById('reviewApproveSection')?.classList.remove('active');
            document.getElementById('reviewRejectSection')?.classList.remove('active');
            document.getElementById('reviewRejectReason').value = '';
            document.getElementById('reviewApproveComment').value = '';
            document.getElementById('reviewRejectComment').value = '';
            document.getElementById('reviewRequestError')?.classList.remove('active');

            document.getElementById('reviewRequestModal')?.classList.add('active');
            document.querySelectorAll('.action-menu.open').forEach(m => m.classList.remove('open'));
        }

        function closeReviewRequestModal() {
            document.getElementById('reviewRequestModal')?.classList.remove('active');
            document.getElementById('reviewRequestError')?.classList.remove('active');
            selectedReviewOrder = {};
        }

        function showReviewRequestToast(message) {
            const toast = document.getElementById('reviewRequestToast');
            const text = document.getElementById('reviewRequestToastMessage');
            if (text) text.textContent = message;
            toast?.classList.add('active');
            window.setTimeout(function () {
                toast?.classList.remove('active');
            }, 2600);
        }

        function openDeclineOrderModal(orderId) {
            const modal = document.getElementById('declineOrderModal');
            const orderText = document.getElementById('declineOrderIdText');
            selectedDeclineOrderId = orderId || '';
            if (orderText) {
                orderText.textContent = selectedDeclineOrderId;
            }
            modal?.classList.add('active');
            document.querySelectorAll('.action-menu.open').forEach(m => m.classList.remove('open'));
        }

        function closeDeclineOrderModal() {
            const modal = document.getElementById('declineOrderModal');
            modal?.classList.remove('active');
        }

        function openDeclineReasonModal() {
            const modal = document.getElementById('declineReasonModal');
            const orderText = document.getElementById('declineReasonOrderIdText');
            const reasonInput = document.getElementById('declineReasonInput');
            const reasonError = document.getElementById('declineReasonError');

            if (orderText) {
                orderText.textContent = selectedDeclineOrderId;
            }
            if (reasonInput) {
                reasonInput.value = '';
                reasonInput.focus();
            }
            if (reasonError) {
                reasonError.style.display = 'none';
            }
            modal?.classList.add('active');
        }

        function closeDeclineReasonModal() {
            const modal = document.getElementById('declineReasonModal');
            modal?.classList.remove('active');
        }

        function openDeclineSuccessModal() {
            const modal = document.getElementById('declineSuccessModal');
            const message = document.getElementById('declineSuccessMessage');
            if (message) {
                message.textContent = 'Order ' + selectedDeclineOrderId + ' has been declined.';
            }
            modal?.classList.add('active');
        }

        function closeDeclineSuccessModal() {
            const modal = document.getElementById('declineSuccessModal');
            modal?.classList.remove('active');
        }

        function closeAllDeclineFlowModals() {
            closeDeclineOrderModal();
            closeDeclineReasonModal();
            closeDeclineSuccessModal();
            selectedDeclineOrderId = '';
        }

        document.addEventListener('click', function (event) {
            const acceptBtn = event.target.closest('.accept-order-btn');
            if (acceptBtn) {
                const orderId = acceptBtn.dataset.orderId || '';
                selectedOrderId = orderId;
                selectedAcceptSummary = {
                    customer: acceptBtn.dataset.customer || '',
                    orderDate: acceptBtn.dataset.orderDate || '',
                    product: acceptBtn.dataset.product || '',
                    quantity: acceptBtn.dataset.quantity || '',
                    payment: acceptBtn.dataset.payment || '',
                    total: acceptBtn.dataset.total || '',
                    contact: acceptBtn.dataset.contact || '',
                    address: acceptBtn.dataset.address || ''
                };
                openCourierSelectionModal();
                return;
            }

            const declineBtn = event.target.closest('.decline-order-btn');
            if (declineBtn) {
                const orderId = declineBtn.dataset.orderId || '';
                openDeclineOrderModal(orderId);
                return;
            }

            const addNoteBtn = event.target.closest('.add-note-btn');
            if (addNoteBtn) {
                const orderId = addNoteBtn.dataset.orderId || '';
                const customer = addNoteBtn.dataset.customer || '';
                openAddNotesModal(orderId, customer);
                return;
            }

            const reviewBtn = event.target.closest('.review-request-btn');
            if (reviewBtn) {
                openReviewRequestModal({
                    orderId: reviewBtn.dataset.orderId || '',
                    buyer: reviewBtn.dataset.buyer || '',
                    product: reviewBtn.dataset.product || '',
                    orderDate: reviewBtn.dataset.orderDate || '',
                    deliveryDate: reviewBtn.dataset.deliveryDate || '',
                    payment: reviewBtn.dataset.payment || ''
                });
                return;
            }

            if (!event.target.closest('.action-menu-wrap')) {
                document.querySelectorAll('.action-menu.open').forEach(m => m.classList.remove('open'));
            }

            const courierOption = event.target.closest('.courier-option');
            if (courierOption) {
                selectedCourier = courierOption.dataset.courier || '';
                document.querySelectorAll('.courier-option').forEach(function (option) {
                    option.classList.remove('active');
                });
                courierOption.classList.add('active');
                const confirmBtn = document.getElementById('courierSelectionConfirmBtn');
                if (confirmBtn) {
                    confirmBtn.disabled = !selectedCourier;
                }
                return;
            }

            const modalOverlay = event.target.closest('.modal-overlay');
            if (modalOverlay && event.target === modalOverlay) {
                if (modalOverlay.id === 'shipmentSummaryModal') {
                    closeAllAcceptFlowModals();
                    return;
                }
                if (modalOverlay.id === 'courierSelectionModal') {
                    closeCourierSelectionModal();
                    return;
                }
                if (modalOverlay.id === 'declineOrderModal') {
                    closeDeclineOrderModal();
                    return;
                }
                if (modalOverlay.id === 'declineReasonModal') {
                    closeDeclineReasonModal();
                    return;
                }
                if (modalOverlay.id === 'declineSuccessModal') {
                    closeAllDeclineFlowModals();
                    return;
                }
            }

            if (event.target && event.target.id === 'addNotesModal') {
                closeAddNotesModal();
            }
            if (event.target && event.target.id === 'reviewRequestModal') {
                closeReviewRequestModal();
            }
        });

        document.querySelectorAll('.order-filter-btn').forEach(function (btn) {
            btn.addEventListener('click', function () {
                activeStatusFilter = btn.dataset.filter;

                document.querySelectorAll('.order-filter-btn').forEach(function (b) {
                    b.classList.remove('active');
                });
                btn.classList.add('active');
                applyOrderFilters();
            });
        });

        document.getElementById('orderSearch')?.addEventListener('input', applyOrderFilters);
        document.getElementById('categoryFilter')?.addEventListener('change', applyOrderFilters);
        document.getElementById('dateFrom')?.addEventListener('change', applyOrderFilters);
        document.getElementById('dateTo')?.addEventListener('change', applyOrderFilters);

        document.getElementById('clearDates')?.addEventListener('click', function () {
            const from = document.getElementById('dateFrom');
            const to = document.getElementById('dateTo');
            if (from) from.value = '';
            if (to) to.value = '';
            applyOrderFilters();
        });

        document.getElementById('courierSelectionCancelBtn')?.addEventListener('click', closeCourierSelectionModal);
        document.getElementById('courierSelectionConfirmBtn')?.addEventListener('click', function () {
            if (!selectedCourier) {
                return;
            }
            closeCourierSelectionModal(false);
            openShipmentSummaryModal();
        });
        document.getElementById('shipmentSummaryCloseBtn')?.addEventListener('click', closeAllAcceptFlowModals);
        document.getElementById('shipmentSummaryCloseBtn2')?.addEventListener('click', closeAllAcceptFlowModals);
        document.getElementById('shipmentSummaryPrintBtn')?.addEventListener('click', printShipmentSummary);
        document.getElementById('declineOrderCancelBtn')?.addEventListener('click', closeAllDeclineFlowModals);
        document.getElementById('declineOrderConfirmBtn')?.addEventListener('click', function () {
            if (!selectedDeclineOrderId) {
                return;
            }
            closeDeclineOrderModal();
            openDeclineReasonModal();
        });
        document.getElementById('declineReasonCancelBtn')?.addEventListener('click', closeAllDeclineFlowModals);
        document.getElementById('declineReasonSubmitBtn')?.addEventListener('click', function () {
            const reasonInput = document.getElementById('declineReasonInput');
            const reasonError = document.getElementById('declineReasonError');
            const reason = (reasonInput?.value || '').trim();

            if (!reason) {
                if (reasonError) {
                    reasonError.style.display = 'block';
                }
                return;
            }

            if (reasonError) {
                reasonError.style.display = 'none';
            }

            closeDeclineReasonModal();
            openDeclineSuccessModal();
        });
        document.getElementById('declineSuccessOkBtn')?.addEventListener('click', closeAllDeclineFlowModals);
        document.getElementById('addNotesCloseBtn')?.addEventListener('click', closeAddNotesModal);
        document.getElementById('addNotesCancelBtn')?.addEventListener('click', closeAddNotesModal);
        document.getElementById('addNotesTextarea')?.addEventListener('input', function () {
            const textarea = document.getElementById('addNotesTextarea');
            const charCount = document.getElementById('addNotesCharCount');
            const error = document.getElementById('addNotesError');
            const length = textarea?.value.length || 0;

            if (charCount) {
                charCount.textContent = String(length);
            }
            if (error && length > 0) {
                error.classList.remove('active');
            }
        });
        document.getElementById('addNotesSaveBtn')?.addEventListener('click', function () {
            const textarea = document.getElementById('addNotesTextarea');
            const error = document.getElementById('addNotesError');
            const value = (textarea?.value || '').trim();

            if (!value) {
                error?.classList.add('active');
                return;
            }

            error?.classList.remove('active');
            closeAddNotesModal();
            showAddNotesToast('Note saved for order ' + selectedNoteOrderId + '.');
        });
        document.getElementById('reviewRequestCloseBtn')?.addEventListener('click', closeReviewRequestModal);
        document.getElementById('reviewRequestCancelBtn')?.addEventListener('click', closeReviewRequestModal);
        document.querySelectorAll('input[name="reviewDecision"]').forEach(function (input) {
            input.addEventListener('change', function () {
                const approveSection = document.getElementById('reviewApproveSection');
                const rejectSection = document.getElementById('reviewRejectSection');
                const isApprove = input.value === 'approve';
                approveSection?.classList.toggle('active', isApprove);
                rejectSection?.classList.toggle('active', !isApprove);
                document.getElementById('reviewRequestError')?.classList.remove('active');
            });
        });
        document.getElementById('reviewRequestSubmitBtn')?.addEventListener('click', function () {
            const selectedDecision = document.querySelector('input[name="reviewDecision"]:checked');
            const rejectReason = document.getElementById('reviewRejectReason')?.value || '';
            const error = document.getElementById('reviewRequestError');

            if (!selectedDecision) {
                error?.classList.add('active');
                return;
            }

            if (selectedDecision.value === 'reject' && !rejectReason) {
                error?.classList.add('active');
                return;
            }

            error?.classList.remove('active');
            const actionText = selectedDecision.value === 'approve' ? 'approved' : 'rejected';
            closeReviewRequestModal();
            showReviewRequestToast('Return request for order ' + (selectedReviewOrder.orderId || '') + ' was ' + actionText + '.');
        });

        document.addEventListener('keydown', function (event) {
            if (event.key === 'Escape') {
                closeAllAcceptFlowModals();
                closeAllDeclineFlowModals();
                closeAddNotesModal();
                closeReviewRequestModal();
            }
        });

        document.addEventListener('DOMContentLoaded', function () {
            applyOrderFilters();
        });
