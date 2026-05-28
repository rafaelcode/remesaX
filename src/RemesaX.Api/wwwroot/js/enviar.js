// =============================================================================
// RemesaX - pantalla de envío de remesa
// =============================================================================

const form = document.getElementById('remittance-form');
const btnSubmit = document.getElementById('btn-submit');
const btnCreateWallet = document.getElementById('btn-create-wallet');
const createdWallet = document.getElementById('created-wallet');
const createdPublicKey = document.getElementById('created-public-key');
const destinationAddress = document.getElementById('destinationAddress');
const amountInput = document.getElementById('amount');
const currencySelect = document.getElementById('targetCurrency');
const previewSend = document.getElementById('preview-send');
const previewReceive = document.getElementById('preview-receive');
const resultBox = document.getElementById('result');

// ----- Preview en tiempo real -----
function updatePreview() {
  const amount = parseFloat(amountInput.value) || 0;
  const currency = currencySelect.value;
  const rate = FX_RATES[currency] || 1;

  previewSend.textContent = formatCurrency(amount, 'USD');
  previewReceive.textContent = formatCurrency(amount * rate, currency);
}

amountInput.addEventListener('input', updatePreview);
currencySelect.addEventListener('change', updatePreview);
updatePreview();

// ----- Crear wallet de prueba -----
btnCreateWallet?.addEventListener('click', async () => {
  btnCreateWallet.disabled = true;
  btnCreateWallet.textContent = 'Creando cuenta + trustline...';
  try {
    // 1. Crear cuenta
    const wallet = await apiCall('/api/Wallet/create', { method: 'POST' });
    // 2. Crear trustline a USDX
    await apiCall('/api/Wallet/trustline', {
      method: 'POST',
      body: JSON.stringify({
        accountSecretKey: wallet.secretKey,
        assetCode: 'USDX'
      })
    });
    // Mostrar
    createdPublicKey.textContent = wallet.publicKey;
    createdWallet.classList.remove('hidden');
    destinationAddress.value = wallet.publicKey;
    btnCreateWallet.textContent = '✅ Wallet creada — copiada al destino';
  } catch (err) {
    btnCreateWallet.disabled = false;
    btnCreateWallet.textContent = 'Crear wallet de prueba';
    alert('Error creando wallet: ' + err.message);
  }
});

// ----- Submit remesa -----
form.addEventListener('submit', async (e) => {
  e.preventDefault();
  resultBox.classList.add('hidden');
  resultBox.classList.remove('success', 'error');
  btnSubmit.disabled = true;
  btnSubmit.textContent = 'Procesando on-chain...';

  const body = {
    senderName: document.getElementById('senderName').value.trim(),
    destinationAddress: destinationAddress.value.trim(),
    amount: parseFloat(amountInput.value),
    targetCurrency: currencySelect.value
  };

  try {
    const result = await apiCall('/api/Remittances', {
      method: 'POST',
      body: JSON.stringify(body)
    });
    showSuccess(result);
  } catch (err) {
    showError(err.message);
  } finally {
    btnSubmit.disabled = false;
    btnSubmit.textContent = 'Enviar remesa';
  }
});

function showSuccess(r) {
  resultBox.innerHTML = `
    <h3>✅ Remesa procesada con éxito</h3>
    <p class="result-detail"><strong>ID:</strong> <code>${r.id}</code></p>
    <p class="result-detail"><strong>Estado:</strong> ${r.status}</p>
    <p class="result-detail"><strong>Enviaste:</strong> ${formatCurrency(r.amountUsdx, 'USD')}</p>
    <p class="result-detail"><strong>Beneficiario recibe:</strong> ${formatCurrency(r.amountLocal, r.targetCurrency)}</p>
    <p class="result-detail"><strong>Hash de transacción:</strong> <code>${r.transactionHash ?? '—'}</code></p>
    ${r.explorerUrl
      ? `<p class="result-detail">🔗 <a href="${r.explorerUrl}" target="_blank" rel="noopener">Ver en Stellar Expert →</a></p>`
      : ''}
  `;
  resultBox.classList.add('success');
  resultBox.classList.remove('hidden');
}

function showError(msg) {
  resultBox.innerHTML = `
    <h3>❌ Error al procesar remesa</h3>
    <p class="result-detail">${msg}</p>
    <p class="result-detail"><small>Verifica que la wallet destino tenga trustline a USDX.</small></p>
  `;
  resultBox.classList.add('error');
  resultBox.classList.remove('hidden');
}
