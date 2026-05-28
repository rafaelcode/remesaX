// =============================================================================
// HelloStellar.cs — "Hola mundo" Stellar desde C# (SDK v14+, namespace nuevo)
// =============================================================================
//
// COMPATIBLE CON: stellar-dotnet-sdk v14.x (paquete actual de Beans-BV)
//
// CAMBIOS vs versiones antiguas:
//   - Namespace antiguo: stellar_dotnet_sdk  ❌
//   - Namespace nuevo:   StellarDotnetSdk.* (PascalCase, varios sub-namespaces)
//
// CÓMO USARLO:
//   1. Crear proyecto:
//        dotnet new console -n HelloStellar
//        cd HelloStellar
//        dotnet add package stellar-dotnet-sdk
//   2. Reemplazar Program.cs con este archivo
//   3. Ejecutar:
//        dotnet run
// =============================================================================

using StellarDotnetSdk;
using StellarDotnetSdk.Accounts;
using StellarDotnetSdk.Assets;
using StellarDotnetSdk.Memos;
using StellarDotnetSdk.Operations;
using StellarDotnetSdk.Responses;
using StellarDotnetSdk.Transactions;

Console.WriteLine("🚀 RemesaX - Hello Stellar (SDK v14)\n");

// Configurar red Testnet (gratis, sin riesgo)
Network.UseTestNetwork();
using var server = new Server("https://horizon-testnet.stellar.org");
var http = new HttpClient();

// -----------------------------------------------------------------------------
// PASO 1: Crear cuenta del REMITENTE
// -----------------------------------------------------------------------------
Console.WriteLine("📝 Paso 1: Creando cuenta del remitente...");
var sender = KeyPair.Random();
Console.WriteLine($"   Public key:  {sender.AccountId}");
Console.WriteLine($"   Secret seed: {sender.SecretSeed}  ⚠️ NUNCA compartas esto en mainnet\n");

await FundWithFriendbotAsync(sender.AccountId);
var senderAccount = await server.Accounts.Account(sender.AccountId);
PrintBalances("Remitente", senderAccount);

// -----------------------------------------------------------------------------
// PASO 2: Crear cuenta del RECEPTOR
// -----------------------------------------------------------------------------
Console.WriteLine("\n📝 Paso 2: Creando cuenta del receptor...");
var receiver = KeyPair.Random();
Console.WriteLine($"   Public key:  {receiver.AccountId}");

await FundWithFriendbotAsync(receiver.AccountId);
var receiverAccount = await server.Accounts.Account(receiver.AccountId);
PrintBalances("Receptor", receiverAccount);

// -----------------------------------------------------------------------------
// PASO 3: Enviar 100 XLM del remitente al receptor
// -----------------------------------------------------------------------------
Console.WriteLine("\n💸 Paso 3: Enviando 100 XLM del remitente al receptor...");

// Recargar cuenta para tener la secuencia actual
senderAccount = await server.Accounts.Account(sender.AccountId);

// Construir operación de pago - en v14 PaymentOperation se instancia directo
var payment = new PaymentOperation(
    destination: receiver,
    asset: new AssetTypeNative(),
    amount: "100");

// Construir transacción
var transaction = new TransactionBuilder(senderAccount)
    .AddOperation(payment)
    .AddMemo(Memo.Text("Hola Stellar desde C#!"))
    .Build();

// Firmar con la llave secreta del remitente
transaction.Sign(sender);

// Enviar a la red
var result = await server.SubmitTransaction(transaction);

if (result != null && result.IsSuccess)
{
    Console.WriteLine($"✅ Transacción exitosa!");
    Console.WriteLine($"   Hash: {result.Hash}");
    Console.WriteLine($"   🔗 Ver en explorador:");
    Console.WriteLine($"   https://stellar.expert/explorer/testnet/tx/{result.Hash}\n");
}
else
{
    Console.WriteLine("❌ Transacción fallida.");
    if (result?.SubmitTransactionResponseExtras?.ResultCodes != null)
    {
        Console.WriteLine($"   Código: {result.SubmitTransactionResponseExtras.ResultCodes.TransactionResultCode}");
    }
}

// -----------------------------------------------------------------------------
// PASO 4: Verificar saldos finales
// -----------------------------------------------------------------------------
await Task.Delay(2000);
Console.WriteLine("📊 Paso 4: Verificando saldos finales...\n");

senderAccount = await server.Accounts.Account(sender.AccountId);
receiverAccount = await server.Accounts.Account(receiver.AccountId);

PrintBalances("Remitente (final)", senderAccount);
PrintBalances("Receptor (final)", receiverAccount);

Console.WriteLine("\n🎉 ¡Listo! Acabas de hacer tu primera transacción en Stellar.");
Console.WriteLine("   Próximo paso: emitir tu propio asset (USDX) y enviarlo.");

// =============================================================================
// HELPERS
// =============================================================================

async Task FundWithFriendbotAsync(string accountId)
{
    Console.WriteLine($"💰 Financiando con friendbot...");
    var url = $"https://friendbot.stellar.org?addr={accountId}";
    var response = await http.GetAsync(url);

    if (!response.IsSuccessStatusCode)
    {
        var body = await response.Content.ReadAsStringAsync();
        throw new Exception($"Friendbot falló ({response.StatusCode}): {body}");
    }

    // Pequeña espera para asegurar propagación en Horizon
    await Task.Delay(2000);
}

static void PrintBalances(string label, AccountResponse account)
{
    Console.WriteLine($"   Saldos de {label}:");
    foreach (var b in account.Balances)
    {
        var asset = b.AssetType == "native" ? "XLM" : b.AssetCode;
        Console.WriteLine($"     - {b.BalanceString} {asset}");
    }
}
