// =============================================================================
// RemesaX - utilidades comunes
// =============================================================================

const API_BASE = ''; // mismo origen que los HTML estáticos

const FX_RATES = {
  MXN: 17.85,
  ARS: 985.50,
  COP: 4150.00,
  PEN: 3.78,
  BRL: 5.05
};

const CURRENCY_SYMBOLS = {
  MXN: 'MX$', ARS: 'AR$', COP: 'CO$', PEN: 'S/', BRL: 'R$'
};

function formatCurrency(value, currency = 'USD') {
  const num = Number(value);
  if (isNaN(num)) return '—';
  if (currency === 'USD') return `$${num.toFixed(2)} USD`;
  const symbol = CURRENCY_SYMBOLS[currency] ?? '';
  return `${symbol}${num.toLocaleString('es-PE', { maximumFractionDigits: 2 })} ${currency}`;
}

function shortenAddress(addr) {
  if (!addr || addr.length < 10) return addr ?? '—';
  return `${addr.substring(0, 6)}...${addr.substring(addr.length - 4)}`;
}

function formatDate(iso) {
  if (!iso) return '—';
  const d = new Date(iso);
  return d.toLocaleString('es-PE', {
    year: 'numeric', month: '2-digit', day: '2-digit',
    hour: '2-digit', minute: '2-digit'
  });
}

async function apiCall(path, options = {}) {
  const url = `${API_BASE}${path}`;
  const headers = { 'Content-Type': 'application/json', ...(options.headers || {}) };
  const res = await fetch(url, { ...options, headers });

  let payload = null;
  try { payload = await res.json(); } catch { /* respuesta no JSON */ }

  if (!res.ok) {
    const errMsg = payload?.error || payload?.title || `HTTP ${res.status}`;
    throw new Error(errMsg);
  }
  return payload;
}
