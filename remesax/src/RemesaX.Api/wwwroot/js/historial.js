// =============================================================================
// RemesaX - dashboard de historial
// =============================================================================

const tbody = document.getElementById('remittances-body');
const btnRefresh = document.getElementById('btn-refresh');

async function loadRemittances() {
  tbody.innerHTML = '<tr><td colspan="7" class="loading">Cargando...</td></tr>';
  try {
    const data = await apiCall('/api/Remittances?take=20');
    renderTable(data);
    renderKpis(data);
  } catch (err) {
    tbody.innerHTML = `<tr><td colspan="7" class="loading">Error: ${err.message}</td></tr>`;
  }
}

function renderTable(items) {
  if (!items || items.length === 0) {
    tbody.innerHTML = '<tr><td colspan="7" class="loading">No hay remesas registradas todavía. <a href="/enviar.html">Envía la primera →</a></td></tr>';
    return;
  }

  tbody.innerHTML = items.map(r => `
    <tr>
      <td>${formatDate(r.createdAt)}</td>
      <td>${escapeHtml(r.senderName)}</td>
      <td><span class="address-short" title="${r.destinationAddress}">${shortenAddress(r.destinationAddress)}</span></td>
      <td>${formatCurrency(r.amountUsdx, 'USD')}</td>
      <td>${formatCurrency(r.amountLocal, r.targetCurrency)}</td>
      <td><span class="badge badge-${r.status.toLowerCase()}">${r.status}</span></td>
      <td>
        ${r.explorerUrl
          ? `<a class="tx-link" href="${r.explorerUrl}" target="_blank" rel="noopener">Ver →</a>`
          : '—'}
      </td>
    </tr>
  `).join('');
}

function renderKpis(items) {
  const total = items.length;
  const success = items.filter(r => r.status === 'Completed').length;
  const failed = items.filter(r => r.status === 'Failed').length;
  const volume = items
    .filter(r => r.status === 'Completed')
    .reduce((sum, r) => sum + Number(r.amountUsdx || 0), 0);

  document.getElementById('kpi-total').textContent = total;
  document.getElementById('kpi-volume').textContent = formatCurrency(volume, 'USD').replace(' USD', '');
  document.getElementById('kpi-success').textContent = success;
  document.getElementById('kpi-failed').textContent = failed;
}

function escapeHtml(str) {
  return String(str ?? '').replace(/[&<>"']/g, c => ({
    '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
  }[c]));
}

btnRefresh?.addEventListener('click', loadRemittances);
loadRemittances();
