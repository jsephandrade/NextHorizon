async function filterLeaderboard(category) {
    const res     = await fetch(`/Admin/GetLeaderboard?category=${category}`);
    const data    = await res.json();
    const tbody   = document.querySelector('.leaderboard-container tbody');

    if (!data.length) {
        tbody.innerHTML = `<tr><td colspan="4" class="text-center text-muted py-3 small">No data for this category.</td></tr>`;
        return;
    }

    tbody.innerHTML = data.map(r => `
        <tr>
            <td class="ps-4">#${r.rank}</td>
            <td class="fw-bold">${r.userName}</td>
            <td>${r.stravaKM} KM</td>
            <td><span class="badge rounded-pill bg-soft-success text-success border border-success" style="font-size:9px;">VERIFIED</span></td>
        </tr>`).join('');
}