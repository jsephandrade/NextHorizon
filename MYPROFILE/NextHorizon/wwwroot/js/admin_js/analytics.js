﻿document.addEventListener('DOMContentLoaded', function () {

    // --- 1. BOOTSTRAP TOOLTIPS ---
    // Initializes all tooltips (used in the Funnel and other UI elements)
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'))
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl)
    });

    // --- 2. DATE PRESET SELECTOR ---
    // Shows or hides custom date inputs based on dropdown selection
    const datePreset = document.getElementById('datePreset');
    const customContainer = document.getElementById('customDateContainer');

    if (datePreset) {
        datePreset.addEventListener('change', function () {
            if (this.value === 'custom') {
                customContainer.classList.remove('d-none');
            } else {
                customContainer.classList.add('d-none');
                // AJAX call logic for preset ranges would go here
                console.log("Fetching data for preset: " + this.value + " days");
            }
        });
    }

    // --- 3. ACTIVITY VS SALES CORRELATION CHART (LINE) ---
    const labelsElement = document.getElementById('jsonLabels');
    const kmElement = document.getElementById('jsonKm');
    const revElement = document.getElementById('jsonRev');

    // Only attempt to render if the data exists on the current page
    if (labelsElement && kmElement && revElement && document.getElementById('activitySalesChart')) {
        const labels = JSON.parse(labelsElement.value);
        const kmData = JSON.parse(kmElement.value);
        const revData = JSON.parse(revElement.value);

        const ctx = document.getElementById('activitySalesChart').getContext('2d');
        new Chart(ctx, {
            type: 'line',
            data: {
                labels: labels,
                datasets: [
                    {
                        label: 'Activity (KM)',
                        data: kmData,
                        borderColor: '#212529', // Dark Gray
                        backgroundColor: 'rgba(33, 37, 41, 0.05)',
                        fill: true,
                        tension: 0.4,
                        yAxisID: 'y',
                        pointRadius: 4,
                        pointBackgroundColor: '#212529'
                    },
                    {
                        label: 'Revenue (₱)',
                        data: revData,
                        borderColor: '#0d6efd', // Bootstrap Blue
                        backgroundColor: 'transparent',
                        fill: false,
                        tension: 0.4,
                        yAxisID: 'y1',
                        pointRadius: 4,
                        pointBackgroundColor: '#0d6efd'
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: {
                    mode: 'index',
                    intersect: false,
                },
                scales: {
                    y: {
                        type: 'linear',
                        display: true,
                        position: 'left',
                        title: { display: true, text: 'Distance (KM)', font: { weight: 'bold' } },
                        grid: { drawOnChartArea: true }
                    },
                    y1: {
                        type: 'linear',
                        display: true,
                        position: 'right',
                        title: { display: true, text: 'Revenue (₱)', font: { weight: 'bold' } },
                        grid: { drawOnChartArea: false } // Prevents messy grid overlap
                    }
                },
                plugins: {
                    legend: {
                        position: 'top',
                        align: 'end',
                        labels: { usePointStyle: true, padding: 20 }
                    },
                    tooltip: {
                        padding: 12,
                        backgroundColor: 'rgba(0, 0, 0, 0.8)',
                        cornerRadius: 8
                    }
                }
            }
        });
    }

    // --- 4. PEAK ACTIVITY CHART (BAR) ---
    const peakCanvas = document.getElementById('peakActivityChart');
    if (peakCanvas) {
        const peakCtx = peakCanvas.getContext('2d');
        new Chart(peakCtx, {
            type: 'bar',
            data: {
                labels: ['12am', '4am', '8am', '12pm', '4pm', '8pm', '11pm'],
                datasets: [{
                    label: 'Active Users',
                    data: [12, 5, 45, 30, 85, 120, 40], // Example static data
                    backgroundColor: '#212529',
                    borderRadius: 5
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: {
                    y: { beginAtZero: true, display: false },
                    x: { grid: { display: false } }
                }
            }
        });
    }
});