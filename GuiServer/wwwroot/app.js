// SignalR connection
let connection = null;
let equityChart = null;
let tradeCount = 0;

// Initialize
document.addEventListener('DOMContentLoaded', () => {
    initializeDates();
    initializeChart();
    initializeSignalR();

    document.getElementById('start-btn').addEventListener('click', startBacktest);
});

function initializeDates() {
    const endDate = new Date();
    const startDate = new Date();
    startDate.setFullYear(startDate.getFullYear() - 1);

    document.getElementById('end-date').valueAsDate = endDate;
    document.getElementById('start-date').valueAsDate = startDate;
}

function initializeChart() {
    const ctx = document.getElementById('equity-chart').getContext('2d');

    equityChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: [],
            datasets: [{
                label: 'Equity',
                data: [],
                borderColor: '#667eea',
                backgroundColor: 'rgba(102, 126, 234, 0.1)',
                borderWidth: 2,
                fill: true,
                tension: 0.4,
                pointRadius: 0,
                pointHoverRadius: 5
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            plugins: {
                legend: {
                    display: false
                },
                tooltip: {
                    mode: 'index',
                    intersect: false,
                    callbacks: {
                        label: function(context) {
                            return 'Equity: $' + context.parsed.y.toFixed(2).replace(/\B(?=(\d{3})+(?!\d))/g, ',');
                        }
                    }
                }
            },
            scales: {
                x: {
                    display: true,
                    title: {
                        display: true,
                        text: 'Time'
                    },
                    ticks: {
                        maxTicksLimit: 10
                    }
                },
                y: {
                    display: true,
                    title: {
                        display: true,
                        text: 'Equity ($)'
                    },
                    ticks: {
                        callback: function(value) {
                            return '$' + value.toFixed(0).replace(/\B(?=(\d{3})+(?!\d))/g, ',');
                        }
                    }
                }
            },
            interaction: {
                intersect: false,
                mode: 'index'
            }
        }
    });
}

async function initializeSignalR() {
    connection = new signalR.HubConnectionBuilder()
        .withUrl('/backtestHub')
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Information)
        .build();

    // Event handlers
    connection.on('OnProgress', (progress, message) => {
        updateProgress(progress, message);
    });

    connection.on('OnTrade', (trade) => {
        addTradeToLog(trade);
    });

    connection.on('OnEquityUpdate', (equityPoint) => {
        updateEquityChart(equityPoint);
    });

    connection.on('OnBacktestComplete', (result) => {
        handleBacktestComplete(result);
    });

    connection.on('OnError', (errorMessage) => {
        alert('Error: ' + errorMessage);
        hideProgress();
        enableStartButton();
    });

    // Connection state handlers
    connection.onreconnecting(() => {
        updateConnectionStatus(false, 'Reconnecting...');
    });

    connection.onreconnected(() => {
        updateConnectionStatus(true, 'Connected');
    });

    connection.onclose(() => {
        updateConnectionStatus(false, 'Disconnected');
    });

    // Start connection
    try {
        await connection.start();
        updateConnectionStatus(true, 'Connected');
        console.log('SignalR connected');
    } catch (err) {
        console.error('SignalR connection error:', err);
        updateConnectionStatus(false, 'Connection failed');
    }
}

function updateConnectionStatus(connected, text) {
    const statusDot = document.querySelector('.status-dot');
    const statusText = document.getElementById('status-text');

    statusDot.className = 'status-dot ' + (connected ? 'connected' : 'disconnected');
    statusText.textContent = text;
}

async function startBacktest() {
    // Get selected tickers
    const tickers = [];
    if (document.getElementById('ticker-nvda').checked) tickers.push('NVDA');
    if (document.getElementById('ticker-tsla').checked) tickers.push('TSLA');
    if (document.getElementById('ticker-aapl').checked) tickers.push('AAPL');

    if (tickers.length === 0) {
        alert('Please select at least one ticker');
        return;
    }

    // Get parameters
    const request = {
        tickers: tickers,
        startDate: document.getElementById('start-date').value,
        endDate: document.getElementById('end-date').value,
        shortPeriod: parseInt(document.getElementById('short-period').value),
        longPeriod: parseInt(document.getElementById('long-period').value),
        initialCapital: parseFloat(document.getElementById('initial-capital').value)
    };

    // Validate
    if (!request.startDate || !request.endDate) {
        alert('Please select start and end dates');
        return;
    }

    if (request.shortPeriod >= request.longPeriod) {
        alert('Short period must be less than long period');
        return;
    }

    // Reset UI
    resetResults();
    showProgress();
    disableStartButton();

    // Start backtest
    try {
        await connection.invoke('StartBacktest', request);
    } catch (err) {
        console.error('Error starting backtest:', err);
        alert('Failed to start backtest: ' + err);
        hideProgress();
        enableStartButton();
    }
}

function resetResults() {
    tradeCount = 0;

    // Clear metrics
    document.getElementById('metric-pnl').textContent = '$0.00';
    document.getElementById('metric-return').textContent = '0.00%';
    document.getElementById('metric-sharpe').textContent = '0.00';
    document.getElementById('metric-drawdown').textContent = '0.00%';
    document.getElementById('metric-trades').textContent = '0';
    document.getElementById('metric-winrate').textContent = '0.00%';

    // Clear chart
    equityChart.data.labels = [];
    equityChart.data.datasets[0].data = [];
    equityChart.update('none');

    // Clear trade log
    const tbody = document.getElementById('trade-tbody');
    tbody.innerHTML = '<tr><td colspan="6" class="no-data">Processing...</td></tr>';
}

function updateProgress(progress, message) {
    const progressFill = document.getElementById('progress-fill');
    const progressText = document.getElementById('progress-text');

    progressFill.style.width = progress + '%';
    progressText.textContent = message || (progress + '%');
}

function showProgress() {
    document.getElementById('progress-container').style.display = 'block';
}

function hideProgress() {
    document.getElementById('progress-container').style.display = 'none';
}

function disableStartButton() {
    const btn = document.getElementById('start-btn');
    btn.disabled = true;
    btn.textContent = 'Running...';
}

function enableStartButton() {
    const btn = document.getElementById('start-btn');
    btn.disabled = false;
    btn.textContent = 'Start Backtest';
}

function addTradeToLog(trade) {
    const tbody = document.getElementById('trade-tbody');

    // Remove "no data" row if it exists
    const noDataRow = tbody.querySelector('.no-data');
    if (noDataRow) {
        noDataRow.parentElement.remove();
    }

    const row = document.createElement('tr');

    const date = new Date(trade.timestamp);
    const timeStr = date.toLocaleString();

    const actionClass = trade.action === 0 ? 'trade-buy' : 'trade-sell';
    const actionText = trade.action === 0 ? 'BUY' : 'SELL';

    const pnlClass = trade.pnl > 0 ? 'positive' : (trade.pnl < 0 ? 'negative' : '');

    row.innerHTML = `
        <td>${timeStr}</td>
        <td>${trade.ticker}</td>
        <td class="${actionClass}">${actionText}</td>
        <td>$${trade.price.toFixed(2)}</td>
        <td>${trade.quantity}</td>
        <td class="${pnlClass}">$${trade.pnl.toFixed(2)}</td>
    `;

    tbody.insertBefore(row, tbody.firstChild);
    tradeCount++;
}

function updateEquityChart(equityPoint) {
    const date = new Date(equityPoint.timestamp);
    const label = date.toLocaleString();

    equityChart.data.labels.push(label);
    equityChart.data.datasets[0].data.push(equityPoint.equity);

    // Limit to last 1000 points for performance
    if (equityChart.data.labels.length > 1000) {
        equityChart.data.labels.shift();
        equityChart.data.datasets[0].data.shift();
    }

    equityChart.update('none');
}

function handleBacktestComplete(result) {
    console.log('Backtest complete:', result);

    // Update metrics
    updateMetric('metric-pnl', formatCurrency(result.totalPnl), result.totalPnl >= 0);
    updateMetric('metric-return', result.totalPnlPercent.toFixed(2) + '%', result.totalPnlPercent >= 0);
    updateMetric('metric-sharpe', result.sharpeRatio.toFixed(3), result.sharpeRatio >= 0);
    updateMetric('metric-drawdown', result.maxDrawdown.toFixed(2) + '%', false);
    updateMetric('metric-trades', result.numberOfTrades, true);
    updateMetric('metric-winrate', result.winRate.toFixed(2) + '%', result.winRate >= 50);

    // Update full equity curve
    equityChart.data.labels = result.equityCurve.map(p => {
        const date = new Date(p.timestamp);
        return date.toLocaleString();
    });
    equityChart.data.datasets[0].data = result.equityCurve.map(p => p.equity);
    equityChart.update();

    hideProgress();
    enableStartButton();

    alert('Backtest completed successfully!');
}

function updateMetric(id, value, isPositive) {
    const element = document.getElementById(id);
    element.textContent = value;

    // Add color class for positive/negative values
    if (typeof isPositive === 'boolean') {
        element.classList.remove('positive', 'negative');
        element.classList.add(isPositive ? 'positive' : 'negative');
    }
}

function formatCurrency(value) {
    return '$' + value.toFixed(2).replace(/\B(?=(\d{3})+(?!\d))/g, ',');
}
