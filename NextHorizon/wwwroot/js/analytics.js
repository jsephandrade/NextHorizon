(() => {
    initializeRevenueGraph();
    initializeTopProductsTable();
})();

function initializeRevenueGraph() {
    const yearSelect = document.getElementById('revenueYearSelect');
    const linePath = document.getElementById('revenueLinePath');
    const areaPath = document.getElementById('revenueAreaPath');
    const pointsGroup = document.getElementById('revenuePointGroup');
    const gridGroup = document.getElementById('revenueGridLines');
    const chartShell = document.querySelector('.revenue-chart-shell[data-chart-data]');

    if (!yearSelect || !linePath || !areaPath || !pointsGroup || !gridGroup || !chartShell) {
        return;
    }

    const rawChartData = chartShell.dataset.chartData;
    if (!rawChartData) {
        return;
    }

    let revenueChartData;
    try {
        revenueChartData = JSON.parse(rawChartData);
    } catch {
        return;
    }

    const width = 1080;
    const height = 320;
    const padding = { top: 18, right: 10, bottom: 16, left: 10 };
    const innerWidth = width - padding.left - padding.right;
    const innerHeight = height - padding.top - padding.bottom;
    const monthCount = 12;
    const horizontalGridLines = 5;

    function sanitizeSeries(series) {
        const values = Array.isArray(series)
            ? series.slice(0, monthCount).map(value => Number(value) || 0)
            : [];

        while (values.length < monthCount) {
            values.push(values.length > 0 ? values[values.length - 1] : 0);
        }

        return values;
    }

    function buildGraphFromSeries(series) {
        const values = sanitizeSeries(series);
        const maxValue = Math.max(...values, 1);
        const minValue = Math.min(...values, 0);
        const valueRange = Math.max(maxValue - minValue, 1);

        gridGroup.innerHTML = '';
        for (let i = 0; i < horizontalGridLines; i += 1) {
            const y = padding.top + (i / (horizontalGridLines - 1)) * innerHeight;
            const line = document.createElementNS('http://www.w3.org/2000/svg', 'line');
            line.setAttribute('x1', String(padding.left));
            line.setAttribute('x2', String(width - padding.right));
            line.setAttribute('y1', String(y));
            line.setAttribute('y2', String(y));
            line.setAttribute('class', 'revenue-grid-line');
            gridGroup.appendChild(line);
        }

        const points = values.map((value, index) => {
            const x = padding.left + (index / (monthCount - 1)) * innerWidth;
            const y = padding.top + ((maxValue - value) / valueRange) * innerHeight;
            return { x, y, value };
        });

        const lineD = points
            .map((point, index) => `${index === 0 ? 'M' : 'L'} ${point.x.toFixed(2)} ${point.y.toFixed(2)}`)
            .join(' ');
        linePath.setAttribute('d', lineD);

        const areaD = `${lineD} L ${(padding.left + innerWidth).toFixed(2)} ${(padding.top + innerHeight).toFixed(2)} L ${padding.left.toFixed(2)} ${(padding.top + innerHeight).toFixed(2)} Z`;
        areaPath.setAttribute('d', areaD);

        pointsGroup.innerHTML = '';
        for (const point of points) {
            const circle = document.createElementNS('http://www.w3.org/2000/svg', 'circle');
            circle.setAttribute('cx', point.x.toFixed(2));
            circle.setAttribute('cy', point.y.toFixed(2));
            circle.setAttribute('r', '4.6');
            circle.setAttribute('class', 'revenue-point');

            const tooltip = document.createElementNS('http://www.w3.org/2000/svg', 'title');
            tooltip.textContent = `PHP ${Math.round(point.value).toLocaleString()}`;
            circle.appendChild(tooltip);
            pointsGroup.appendChild(circle);
        }
    }

    function applyGraph(yearKey) {
        const chart = revenueChartData[yearKey];
        if (!chart) {
            return;
        }

        if (Array.isArray(chart)) {
            buildGraphFromSeries(chart);
            return;
        }

        linePath.setAttribute('d', chart.linePath || '');
        areaPath.setAttribute('d', chart.areaPath || '');
        gridGroup.innerHTML = chart.gridMarkup || '';
        pointsGroup.innerHTML = chart.pointsMarkup || '';
    }

    yearSelect.addEventListener('change', () => {
        applyGraph(yearSelect.value);
    });

    const defaultYear = yearSelect.value || Object.keys(revenueChartData)[0];
    if (defaultYear) {
        applyGraph(defaultYear);
    }
}

function initializeTopProductsTable() {
    const tableWrap = document.querySelector('.analytics-table-wrap[data-products]');
    const tableBody = document.querySelector('[data-analytics-products]');
    const rangeButtons = Array.from(document.querySelectorAll('.analytics-range-toggle button[data-range]'));
    const pageButtons = Array.from(document.querySelectorAll('.analytics-pagination .analytics-page-btn[data-page]'));
    const nextButton = document.querySelector('.analytics-pagination .analytics-next-btn');

    if (!tableWrap || !tableBody || rangeButtons.length === 0 || pageButtons.length === 0 || !nextButton) {
        return;
    }

    const rawProducts = tableWrap.dataset.products;
    if (!rawProducts) {
        return;
    }

    let products;
    try {
        products = JSON.parse(rawProducts);
    } catch {
        return;
    }

    if (!Array.isArray(products)) {
        return;
    }

    const rangeConfig = {
        '1H': { unitFactor: 0.2, revenueFactor: 0.2 },
        '1D': { unitFactor: 1, revenueFactor: 1 },
        '7D': { unitFactor: 7, revenueFactor: 7 },
        '1M': { unitFactor: 30, revenueFactor: 30 }
    };

    let activeRange = '1H';
    let activePage = 1;
    const fixedPageSlots = pageButtons.length;
    let lastPageCount = 1;

    function escapeHtml(value) {
        return String(value)
            .replaceAll('&', '&amp;')
            .replaceAll('<', '&lt;')
            .replaceAll('>', '&gt;')
            .replaceAll('"', '&quot;')
            .replaceAll("'", '&#39;');
    }

    function safeText(value, fallback) {
        const text = String(value ?? '').trim();
        return text.length > 0 ? text : fallback;
    }

    function toNumber(value) {
        const parsed = Number(value);
        return Number.isFinite(parsed) ? parsed : 0;
    }

    function getRangeRows(rangeKey) {
        const config = rangeConfig[rangeKey] || rangeConfig['1D'];

        return products
            .map((product) => {
                const baseUnits = Math.max(0, Math.round(toNumber(product.unitsSold)));
                const baseRevenue = toNumber(product.revenueGenerated) > 0
                    ? toNumber(product.revenueGenerated)
                    : baseUnits * 650;

                return {
                    productName: safeText(product.productName, 'Untitled Product'),
                    imageUrl: safeText(product.imageUrl, 'https://via.placeholder.com/60?text=Product'),
                    sku: safeText(product.sku, '-'),
                    category: safeText(product.category, 'General'),
                    unitsSold: Math.max(0, Math.round(baseUnits * config.unitFactor)),
                    revenueGenerated: Math.max(0, Math.round(baseRevenue * config.revenueFactor))
                };
            })
            .sort((a, b) => {
                if (b.unitsSold !== a.unitsSold) {
                    return b.unitsSold - a.unitsSold;
                }

                if (b.revenueGenerated !== a.revenueGenerated) {
                    return b.revenueGenerated - a.revenueGenerated;
                }

                return a.productName.localeCompare(b.productName);
            });
    }

    function formatCurrency(value) {
        return new Intl.NumberFormat('en-PH', {
            minimumFractionDigits: 0,
            maximumFractionDigits: 0
        }).format(value);
    }

    function updatePaginationState(pageCount) {
        lastPageCount = Math.max(1, pageCount);

        pageButtons.forEach((button, index) => {
            const pageNumber = index + 1;
            const isAvailable = pageNumber <= lastPageCount;

            button.disabled = !isAvailable;
            button.setAttribute('aria-disabled', String(!isAvailable));
            button.classList.toggle('active', isAvailable && pageNumber === activePage);
        });

        nextButton.disabled = lastPageCount <= 1;
        nextButton.setAttribute('aria-disabled', String(lastPageCount <= 1));
        nextButton.setAttribute('aria-label', activePage >= lastPageCount ? 'Go back to page 1' : 'Go to next page');
    }

    function renderRows() {
        const rows = getRangeRows(activeRange);
        if (rows.length === 0) {
            tableBody.innerHTML = '<tr><td colspan="6">No products available for this range.</td></tr>';
            activePage = 1;
            updatePaginationState(1);
            return;
        }

        const pageSize = Math.max(1, Math.ceil(rows.length / fixedPageSlots));
        const pageCount = Math.max(1, Math.ceil(rows.length / pageSize));
        activePage = Math.min(Math.max(activePage, 1), pageCount);

        const start = (activePage - 1) * pageSize;
        const pageRows = rows.slice(start, start + pageSize);

        tableBody.innerHTML = pageRows
            .map((row, index) => {
                const rank = start + index + 1;

                return `
                    <tr>
                        <td>${rank}</td>
                        <td>
                            <div class="analytics-product-cell">
                                <img src="${escapeHtml(row.imageUrl)}" alt="${escapeHtml(row.productName)}" onerror="this.src='https://via.placeholder.com/60?text=Product'" />
                                <span>${escapeHtml(row.productName)}</span>
                            </div>
                        </td>
                        <td>${escapeHtml(row.sku)}</td>
                        <td>${escapeHtml(row.category)}</td>
                        <td>${row.unitsSold}</td>
                        <td>&#8369;${formatCurrency(row.revenueGenerated)}</td>
                    </tr>
                `;
            })
            .join('');

        updatePaginationState(pageCount);
    }

    rangeButtons.forEach((button) => {
        button.addEventListener('click', () => {
            activeRange = button.dataset.range || '1H';
            activePage = 1;

            rangeButtons.forEach((candidate) => {
                candidate.classList.toggle('active', candidate === button);
            });

            renderRows();
        });
    });

    pageButtons.forEach((button) => {
        button.addEventListener('click', () => {
            const requestedPage = Number(button.dataset.page);
            if (!Number.isFinite(requestedPage)) {
                return;
            }

            activePage = Math.min(Math.max(1, requestedPage), lastPageCount);
            renderRows();
        });
    });

    nextButton.addEventListener('click', () => {
        if (lastPageCount <= 1) {
            return;
        }

        activePage = activePage >= lastPageCount ? 1 : activePage + 1;
        renderRows();
    });

    const initiallyActiveRangeButton = rangeButtons.find((button) => button.classList.contains('active')) || rangeButtons[0];
    if (initiallyActiveRangeButton) {
        activeRange = initiallyActiveRangeButton.dataset.range || '1H';
        rangeButtons.forEach((button) => {
            button.classList.toggle('active', button === initiallyActiveRangeButton);
        });
    }

    renderRows();
}
