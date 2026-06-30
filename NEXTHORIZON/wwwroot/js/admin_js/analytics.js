﻿let mainChart = null;
let peakChart = null;

async function fetchAndUpdateCharts(days, startDate, endDate) {
    let url = `/Admin/GetAnalyticsData?days=${days}`;
    if (startDate && endDate) url = `/Admin/GetAnalyticsData?startDate=${startDate}&endDate=${endDate}`;

    try {
        const res  = await fetch(url);
        const data = await res.json();

        // Update Avg Order Value card
        const avgEl = document.getElementById('avgOrderValue');
        if (avgEl) avgEl.innerText = '₱' + Number(data.avgOrderValue ?? 0)
            .toLocaleString('en-PH', { minimumFractionDigits: 2 });

        // Update main chart — always clear and redraw
        if (mainChart) {
            const hasData = data.trends && data.trends.length > 0;
            mainChart.data.labels           = hasData ? data.trends.map(t => t.dateLabel) : ['No Data'];
            mainChart.data.datasets[0].data = hasData ? data.trends.map(t => t.challengeParticipants) : [0];
            mainChart.data.datasets[1].data = hasData ? data.trends.map(t => t.totalRevenue) : [0];
            mainChart.update();
        }

        // Update peak chart — always clear and redraw
        if (peakChart) {
            const hasPeak = data.peakData && data.peakData.length > 0;
            peakChart.data.labels           = hasPeak
                ? data.peakData.map(h => {
                    const ampm = h.hour >= 12 ? 'PM' : 'AM';
                    const hour = h.hour % 12 || 12;
                    return hour + ampm;
                })
                : ['No Data'];
            peakChart.data.datasets[0].data = hasPeak ? data.peakData.map(h => h.activitySyncCount) : [0];
            peakChart.data.datasets[1].data = hasPeak ? data.peakData.map(h => h.purchaseCount)     : [0];
            peakChart.update();
        }

        // Update top products list
        const productList = document.querySelector('.product-list');
        if (productList) {
            if (data.topProducts && data.topProducts.length > 0) {
                productList.innerHTML = data.topProducts.map(p => `
                    <div class="product-item d-flex align-items-center gap-3 mb-3 p-2 rounded-3 border-bottom pb-3">
                        <div class="avatar-placeholder bg-soft-primary rounded-3 d-flex align-items-center justify-content-center"
                             style="width: 45px; height: 45px; flex-shrink: 0;">
                            <i class="bi bi-box-seam text-primary"></i>
                        </div>
                        <div class="flex-grow-1">
                            <p class="mb-0 fw-bold small">${escapeHtml(p.productName)}</p>
                            <p class="text-muted tiny mb-0">${p.unitsSold} Units Sold</p>
                            <p class="text-muted tiny mb-0">${escapeHtml(p.sellerName)}</p>
                        </div>
                        <div class="text-end">
                            <span class="fw-800 text-dark d-block">₱${Number(p.revenue).toLocaleString('en-PH', { minimumFractionDigits: 0 })}</span>
                            <span class="tiny text-uppercase fw-bold text-muted">SALES</span>
                        </div>
                    </div>
                `).join('');
            } else {
                productList.innerHTML = '<p class="text-muted small fst-italic">No product data available for this period.</p>';
            }
        }

    } catch(e) {
        console.error('Failed to fetch analytics data', e);
    }
}

function escapeHtml(str) {
    const div = document.createElement('div');
    div.appendChild(document.createTextNode(str ?? ''));
    return div.innerHTML;
}

document.addEventListener('DOMContentLoaded', function () {

    // --- DATE PRESET SELECTOR ---
    const datePreset     = document.getElementById('datePreset');
    const customContainer = document.getElementById('customDateContainer');
    const startDateInput = document.getElementById('startDate');
    const endDateInput   = document.getElementById('endDate');

    if (datePreset) {
        datePreset.addEventListener('change', function () {
            if (this.value === 'custom') {
                customContainer.classList.remove('d-none');
            } else {
                customContainer.classList.add('d-none');
                fetchAndUpdateCharts(parseInt(this.value));
            }
        });
    }

    if (startDateInput && endDateInput) {
        [startDateInput, endDateInput].forEach(input => {
            input.addEventListener('change', () => {
                const s = startDateInput.value;
                const e = endDateInput.value;
                if (s && e) fetchAndUpdateCharts(0, s, e);
            });
        });
    }

    // --- MAIN CHART ---
    const labelsEl        = document.getElementById('jsonLabels');
    const participantsEl  = document.getElementById('jsonParticipants');
    const revEl           = document.getElementById('jsonRevenue');
    const mainCanvas      = document.getElementById('mainAnalyticsChart');

    if (labelsEl && participantsEl && revEl && mainCanvas) {
        const labels          = JSON.parse(labelsEl.value);
        const participantData = JSON.parse(participantsEl.value);
        const revenueData     = JSON.parse(revEl.value);

        mainChart = new Chart(mainCanvas.getContext('2d'), {
            type: 'line',
            data: {
                labels,
                datasets: [
                    {
                        label: 'Challengers Joined',
                        data: participantData,
                        borderColor: '#212529', backgroundColor: '#212529',
                        fill: false, tension: 0.4, yAxisID: 'y',
                        pointRadius: 4, pointBackgroundColor: '#212529'
                    },
                    {
                        label: 'Revenue (₱)',
                        data: revenueData,
                        borderColor: '#0d6efd', backgroundColor: '#0d6efd',
                        fill: false, tension: 0.4, yAxisID: 'y1',
                        pointRadius: 4, pointBackgroundColor: '#0d6efd'
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                scales: {
                    y:  { type: 'linear', position: 'left',  title: { display: true, text: 'Consumers' } },
                    y1: { type: 'linear', position: 'right', grid: { drawOnChartArea: false }, title: { display: true, text: 'Revenue (₱)' } }
                },
                plugins: {
                    legend: {
                        position: 'top', align: 'end',
                        labels: { usePointStyle: true, pointStyle: 'circle', padding: 20 }
                    }
                }
            }
        });
    }

    // --- PEAK CHART ---
    const peakCanvas      = document.getElementById('peakActivityChart');
    const peakHoursEl     = document.getElementById('peakHours');
    const peakSyncsEl     = document.getElementById('peakSyncs');
    const peakPurchasesEl = document.getElementById('peakPurchases');

    if (peakCanvas && peakHoursEl) {
        const peakLabels    = JSON.parse(peakHoursEl.value).map(h => {
            const ampm = h >= 12 ? 'PM' : 'AM';
            const hour = h % 12 || 12;
            return hour + ampm;
        });
        const peakSyncs     = JSON.parse(peakSyncsEl.value);
        const peakPurchases = JSON.parse(peakPurchasesEl.value);

        peakChart = new Chart(peakCanvas.getContext('2d'), {
            type: 'bar',
            data: {
                labels: peakLabels,
                datasets: [
                    { label: 'App Activity', data: peakSyncs,     backgroundColor: '#212529', borderRadius: 5 },
                    { label: 'Purchases',    data: peakPurchases, backgroundColor: '#0d6efd', borderRadius: 5 }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: true, position: 'bottom', labels: { usePointStyle: true, padding: 15 } }
                },
                scales: {
                    x: { grid: { display: false } },
                    y: { beginAtZero: true }
                }
            }
        });
    }
     fetchAndUpdateCharts(30);
});

function exportAnalyticsToExcel() {
    const date = new Date().toLocaleDateString('en-US', { year:'numeric', month:'long', day:'numeric' });

    // Collect trend data from chart
    const trendRows = [['Date', 'Challengers Joined', 'Revenue (₱)']];
    if (mainChart) {
        mainChart.data.labels.forEach((label, i) => {
            trendRows.push([
                label,
                mainChart.data.datasets[0].data[i] ?? 0,
                mainChart.data.datasets[1].data[i] ?? 0
            ]);
        });
    }

    // Collect peak engagement data from chart
    const peakRows = [['Hour', 'App Activity', 'Purchases']];
    if (peakChart) {
        peakChart.data.labels.forEach((label, i) => {
            peakRows.push([
                label,
                peakChart.data.datasets[0].data[i] ?? 0,
                peakChart.data.datasets[1].data[i] ?? 0
            ]);
        });
    }

    // Collect top sellers
    const sellerRows = [['Seller Name', 'Orders', 'Revenue']];
    document.querySelectorAll('.seller-item').forEach(item => {
        const name    = item.querySelector('.fw-bold.small')?.innerText?.trim() ?? '';
        const orders  = item.querySelector('.text-muted.tiny')?.innerText?.trim() ?? '';
        const revenue = item.querySelector('.fw-800.text-dark')?.innerText?.trim() ?? '';
        if (name) sellerRows.push([name, orders, revenue]);
    });

    // Collect top products
    const productRows = [['Product Name', 'Units Sold', 'Revenue']];
    document.querySelectorAll('.product-item').forEach(item => {
        const name     = item.querySelector('.fw-bold.small')?.innerText?.trim() ?? '';
        const units    = item.querySelector('.text-muted.tiny')?.innerText?.trim() ?? '';
        const revenue  = item.querySelector('.fw-800.text-dark')?.innerText?.trim() ?? '';
        if (name) productRows.push([name, units, revenue]);
    });

    // Collect stat cards
    const totalConsumers = document.querySelector('.analytics-card:nth-child(1) h2')?.innerText?.trim() ?? 'N/A';
    const totalSellers   = document.querySelector('.analytics-card:nth-child(2) h2')?.innerText?.trim() ?? 'N/A';
    const avgOrder       = document.getElementById('avgOrderValue')?.innerText?.trim() ?? 'N/A';

    const html = `
        <html xmlns:o="urn:schemas-microsoft-com:office:office"
              xmlns:x="urn:schemas-microsoft-com:office:excel"
              xmlns="http://www.w3.org/TR/REC-html40">
        <head><meta charset="UTF-8"></head>
        <body>
            <h2>Next Horizon - Analytics Report</h2>
            <p>Generated: ${date}</p>

            <h4>Platform Summary</h4>
            <table border="1">
                <tr><td><b>Total Consumers</b></td><td>${totalConsumers}</td></tr>
                <tr><td><b>Total Sellers</b></td><td>${totalSellers}</td></tr>
                <tr><td><b>Avg Order Value</b></td><td>${avgOrder}</td></tr>
            </table>

            <br/>

            <h4>Challenge Participation vs Revenue Trend</h4>
            <table border="1">
                ${trendRows.map(row => `<tr>${row.map(c => `<td>${c}</td>`).join('')}</tr>`).join('')}
            </table>

            <br/>

            <h4>Top Performing Sellers</h4>
            <table border="1">
                ${sellerRows.map(row => `<tr>${row.map(c => `<td>${c}</td>`).join('')}</tr>`).join('')}
            </table>

            <br/>

            <h4>Most Purchased Products</h4>
            <table border="1">
                ${productRows.map(row => `<tr>${row.map(c => `<td>${c}</td>`).join('')}</tr>`).join('')}
            </table>

            <br/>

            <h4>Peak Engagement Times</h4>
            <table border="1">
                ${peakRows.map(row => `<tr>${row.map(c => `<td>${c}</td>`).join('')}</tr>`).join('')}
            </table>
        </body>
        </html>`;

    const blob = new Blob([html], { type: 'application/vnd.ms-excel' });
    const url  = URL.createObjectURL(blob);
    const a    = document.createElement('a');
    a.href     = url;
    a.download = `NH_Analytics_Report_${new Date().toISOString().slice(0,10)}.xls`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
}