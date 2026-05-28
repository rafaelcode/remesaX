# 🚀 HelloStellar — Guía de arranque rápido (5 minutos)

Este es tu primer contacto con el SDK de Stellar desde .NET. Sigue estos pasos exactos.

## ⚠️ Compatibilidad

Este script funciona con `stellar-dotnet-sdk` **v14.x** o superior. El namespace cambió a partir de la versión 14, ahora es `StellarDotnetSdk.*` (PascalCase), no `stellar_dotnet_sdk` (con guiones bajos).

## Pasos

```bash
# 1. Crear proyecto consola nuevo
dotnet new console -n HelloStellar
cd HelloStellar

# 2. Agregar el SDK (versión actual)
dotnet add package stellar-dotnet-sdk

# 3. Reemplazar Program.cs con el contenido de HelloStellar.cs
#    (Borra el Program.cs que viene por defecto y copia el contenido del script)

# 4. Ejecutar
dotnet run
```

## ¿Qué deberías ver?

```
🚀 RemesaX - Hello Stellar (SDK v14)

📝 Paso 1: Creando cuenta del remitente...
   Public key:  GABCDEFG12345...
   Secret seed: SHIJKLMN67890...  ⚠️ NUNCA compartas esto en mainnet

💰 Financiando con friendbot...
   Saldos de Remitente:
     - 10000.0000000 XLM

📝 Paso 2: Creando cuenta del receptor...
   ...

💸 Paso 3: Enviando 100 XLM del remitente al receptor...
✅ Transacción exitosa!
   Hash: a3f4d5e6b7c8d9...
   🔗 Ver en explorador:
   https://stellar.expert/explorer/testnet/tx/a3f4d5e6...

📊 Paso 4: Verificando saldos finales...
   ...

🎉 ¡Listo! Acabas de hacer tu primera transacción en Stellar.
```

## Troubleshooting

### Error: "The type or namespace name 'StellarDotnetSdk' could not be found"

→ El paquete no se instaló. Ejecuta nuevamente:
```bash
dotnet add package stellar-dotnet-sdk
dotnet restore
```

### Error: "The type or namespace name 'stellar_dotnet_sdk' could not be found"

→ Estás usando los `using` antiguos. Verifica que el archivo arriba tiene:
```csharp
using StellarDotnetSdk;
using StellarDotnetSdk.Accounts;
// ... etc, todos en PascalCase
```

### Error: "Friendbot failed (429)"

→ Estás haciendo muchas peticiones muy rápido. Espera 1-2 minutos y reintenta. Friendbot tiene rate limit.

### La consola se cuelga en "Financiando con friendbot..."

→ Probablemente tu red bloquea HTTPS hacia stellar.org. Prueba:
```bash
curl https://friendbot.stellar.org?addr=GABC...
```
Si esto falla, es problema de red local (firewall, proxy corporativo, VPN).

### Error: "destination account does not exist"

→ La cuenta destino no fue financiada. Asegúrate de que el `Task.Delay(2000)` se ejecutó después del friendbot. Si tu red es lenta, sube el delay a 4000ms.

### Error de compilación: "PaymentOperation does not contain a constructor that takes X arguments"

→ Tu paquete está en una versión mayor diferente. Verifica:
```bash
dotnet list package
```
Debería mostrar `stellar-dotnet-sdk 14.x.x`. Si es otra versión mayor (15+), las APIs pueden haber cambiado nuevamente y deberás consultar:
https://github.com/Beans-BV/dotnet-stellar-sdk/tree/master/Examples

## Siguiente paso

Una vez que veas el ✅ y abras el link del explorador con tu transacción confirmada:

1. **Captura screenshot** del explorador → es el primer hito de tu portfolio
2. **Guarda las dos secret seeds** que se imprimieron en consola → las usarás como cuentas de prueba para el resto del MVP
3. **Modifica el script** para emitir tu propio asset USDX (ver `docs/GUIA_PRIMERA_SEMANA.md` día 3)
