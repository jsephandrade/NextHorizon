﻿document.addEventListener('DOMContentLoaded', async () => {
    const ctx = document.getElementById('revenueShareChart').getContext('2d');

    let top3Percent   = 0;
    let othersPercent = 100;

    try {
        const res  = await fetch('/Admin/GetSellerPerformance');
        const data = await res.json();

        if (data.topSellers && data.topSellers.length > 0) {
            const totalRevenue = data.topSellers.reduce((sum, s) => sum + s.revenueGenerated, 0);
            const top3Revenue  = data.topSellers.slice(0, 3).reduce((sum, s) => sum + s.revenueGenerated, 0);
            top3Percent   = totalRevenue > 0 ? Math.round((top3Revenue / totalRevenue) * 100) : 0;
            othersPercent = 100 - top3Percent;
        }
    } catch (e) {
        console.error('Failed to load performance data', e);
    }

    // Update legend labels
    document.getElementById('top3-percent').textContent   = top3Percent + '%';
    document.getElementById('others-percent').textContent = othersPercent + '%';

    new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: ['Top 3 Sellers', 'Others'],
            datasets: [{
                data: [top3Percent, othersPercent],
                backgroundColor: ['#212529', '#dee2e6'],
                hoverOffset: 0,
                borderWidth: 0,
            }]
        },
        options: {
            cutout: '88%',
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { display: false },
                tooltip: {
                    callbacks: {
                        label: function (item) {
                            return ` ${item.label}: ${item.raw}%`;
                        }
                    }
                }
            }
        }
    });

    // Handle URL hash tab switching
    const hash = window.location.hash;
    if (hash) {
        const targetTab = document.querySelector('[data-bs-target="' + hash + '"]');
        if (targetTab) {
            new bootstrap.Tab(targetTab).show();
            targetTab.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
    }
});

function viewSellerDetails(name, shop, revenue, orders) {
    document.getElementById('detSellerName').innerText = name;
    document.getElementById('detShopName').innerText   = shop;
    document.getElementById('detRevenue').innerText    = revenue;
    document.getElementById('detOrders').innerText     = orders;
    new bootstrap.Modal(document.getElementById('sellerDetailModal')).show();
}

function exportToExcel() {
    // Build Top Sellers sheet data
    const sellersRows = [
        ['Rank', 'Shop Name', 'Orders Fulfilled', 'Gross Revenue']
    ];
    document.querySelectorAll('#tab-sellers tbody tr').forEach(row => {
        const cells = row.querySelectorAll('td');
        if (cells.length >= 3) {
            sellersRows.push([
                cells[0].innerText.trim(),
                cells[1].innerText.trim(),
                cells[2].innerText.trim(),
                cells[3] ? cells[3].innerText.trim() : ''  // revenue not in table but we skip
            ]);
        }
    });

    // Build Top Products sheet data
    const productsRows = [
        ['Rank', 'Product Name', 'Category', 'Units Sold', 'Total Revenue']
    ];
    document.querySelectorAll('#tab-products tbody tr').forEach(row => {
        const cells = row.querySelectorAll('td');
        if (cells.length >= 4) {
            productsRows.push([
                cells[0].innerText.trim(),
                cells[1].innerText.trim(),
                cells[2].innerText.trim(),
                cells[3].innerText.trim(),
                cells[4] ? cells[4].innerText.trim() : ''
            ]);
        }
    });

    // Convert to CSV-style HTML table for Excel
    let html = `
        <html xmlns:o="urn:schemas-microsoft-com:office:office" 
              xmlns:x="urn:schemas-microsoft-com:office:excel"
              xmlns="http://www.w3.org/TR/REC-html40">
        <head><meta charset="UTF-8"></head>
        <body>
            <h3>Next Horizon - Marketplace Performance Report</h3>
            <p>Generated: ${new Date().toLocaleDateString('en-US', { year:'numeric', month:'long', day:'numeric' })}</p>
            
            <h4>Top Sellers</h4>
            <table border="1">
                ${sellersRows.map(row => `<tr>${row.map(cell => `<td>${cell}</td>`).join('')}</tr>`).join('')}
            </table>
            
            <br/>
            
            <h4>Top Moving Products</h4>
            <table border="1">
                ${productsRows.map(row => `<tr>${row.map(cell => `<td>${cell}</td>`).join('')}</tr>`).join('')}
            </table>
        </body>
        </html>`;

    const blob = new Blob([html], { type: 'application/vnd.ms-excel' });
    const url  = URL.createObjectURL(blob);
    const a    = document.createElement('a');
    a.href     = url;
    a.download = `NH_Performance_Report_${new Date().toISOString().slice(0,10)}.xls`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
}