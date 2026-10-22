# =====================================================================
#  Carga datos de prueba en la API y revisa que responda como debe.
#
#  1. En una terminal:  dotnet run
#  2. En otra, dentro de la carpeta del proyecto:
#        powershell -ExecutionPolicy Bypass -File .\probar.ps1
#
#  Si lo corres otra vez no duplica nada: detecta que ya están y solo repite las revisiones.
#  Para empezar de cero: para la API, borra qrs.db y corre "dotnet ef database update".
# =====================================================================

$ErrorActionPreference = "Stop"
$api = "http://localhost:5087"

function Api($metodo, $ruta, $cuerpo = $null) {
    $p = @{ Method = $metodo; Uri = "$api$ruta" }
    if ($null -ne $cuerpo) {
        $json = ConvertTo-Json -InputObject $cuerpo -Depth 8
        $p.Body = [Text.Encoding]::UTF8.GetBytes($json)   # para que los acentos lleguen bien
        $p.ContentType = "application/json; charset=utf-8"
    }
    $r = Invoke-RestMethod @p
    return , $r
}

$script:bien = 0
$script:mal = 0
function Revisar($titulo, [bool]$ok, $detalle = "") {
    if ($ok) { $script:bien++; Write-Host "  OK     $titulo" -ForegroundColor Green }
    else { $script:mal++; Write-Host "  FALLA  $titulo $detalle" -ForegroundColor Red }
}
# Llama algo que DEBE fallar con cierto código (400, 409…)
function Debe($codigo, $titulo, $metodo, $ruta, $cuerpo = $null) {
    try {
        Api $metodo $ruta $cuerpo | Out-Null
        Revisar "$titulo" $false "(lo aceptó y debía responder $codigo)"
    }
    catch {
        $real = 0
        if ($_.Exception.Response) { $real = [int]$_.Exception.Response.StatusCode }
        $msg = $_.ErrorDetails.Message
        Revisar "$titulo  [$codigo]" ($real -eq $codigo) "(respondió $real $msg)"
        if ($real -eq $codigo -and $msg) {
            try { Write-Host "         $((ConvertFrom-Json $msg).error)" -ForegroundColor DarkGray } catch {}
        }
    }
}

# ---------- ¿Está viva? ----------
try { $cat = Api GET "/api/catalogos" }
catch {
    Write-Host "No encuentro la API en $api. ¿Está corriendo 'dotnet run' en otra terminal? ¿Es ese el puerto?" -ForegroundColor Red
    exit 1
}
Write-Host "`nAPI encontrada en $api`n" -ForegroundColor Cyan

# ---------- Catálogos de prueba ----------
function Asegurar($ruta, $nombre) {
    $lista = Api GET "/api/$ruta"
    $hay = $lista | Where-Object { $_.nombre -eq $nombre } | Select-Object -First 1
    if ($hay) { return $hay.id }
    return (Api POST "/api/$ruta" @{ nombre = $nombre }).id
}
$prov = @{}
foreach ($n in "José Juan Pérez", "Rica Hernández", "María López", "Raúl Hernández", "Esteban Ruiz") { $prov[$n.Split(" ")[0]] = Asegurar "proveedores" $n }
$proc = @{}
foreach ($n in "Bordado", "Lavandería", "Estampado", "Costura") { $proc[$n] = Asegurar "procesos" $n }

$cat = Api GET "/api/catalogos"
# (no se llama "Cli" porque en PowerShell eso ya es un atajo de Clear-Item)
function IdCliente($nombre) { ($cat.clientes | Where-Object { $_.nombre -eq $nombre } | Select-Object -First 1).id }

# ---------- Ayudas ----------
function T($talla, $pedidas, $primeras = 0, $segundas = 0) { @{ talla = $talla; pedidas = $pedidas; primeras = $primeras; segundas = $segundas } }
function L($cantidad, $precio, $talla, $motivo) { @{ cantidad = $cantidad; precioCentavos = $precio; talla = $talla; motivo = $motivo } }
function Dia($dias, $hora = 10) { (Get-Date).Date.AddDays($dias).AddHours($hora).ToString("yyyy-MM-dd'T'HH:mm") }

$opIds = @{}
function Orden($empresa, $op, $modelo, $cliente, $np, $proceso, $proveedor, $precio, $precio2, $tallas) {
    $c = @{
        np = $np; procesoId = $proc[$proceso]; proveedorId = $prov[$proveedor]
        precioCentavos = $precio; precioSegundaCentavos = $precio2; tallas = @($tallas)
    }
    $llave = "$empresa-$op"
    if ($opIds.ContainsKey($llave)) { $c.produccionId = $opIds[$llave] }
    else { $c.nuevaProduccion = @{ empresaId = $empresa; op = $op; modelo = $modelo; clienteId = (IdCliente $cliente) } }
    $r = Api POST "/api/ordenes" $c
    if ($r.produccion) { $opIds[$llave] = $r.produccion.id }
    return $r.orden.id
}
function Conteo($id, $tallas, $lotes = @(), $comentario = $null) {
    Api PATCH "/api/ordenes/$id" @{ tallas = @($tallas); lotesSegundas = @($lotes); comentario = $comentario } | Out-Null
}
function Enlace($id) { Api POST "/api/ordenes/$id/enlace" | Out-Null }
function Cita($id, $fecha) { Api PUT "/api/ordenes/$id/cita" @{ citaConfirmada = $fecha } | Out-Null }

# ---------- Datos de prueba (los mismos casos que la vista previa) ----------
$prods = Api GET "/api/producciones"
if ($prods | Where-Object { $_.empresaId -eq 3 -and $_.op -eq 990 }) {
    Write-Host "Ya hay datos de prueba (OP 990 de Cotton & Print), no los cargo otra vez.`n" -ForegroundColor Yellow
}
else {
    Write-Host "Cargando datos de prueba..." -ForegroundColor Cyan

    # Cotton & Print · OP 990 · ya completada
    $o = Orden 3 990 "GORRAESC" "Chedraui" 4410 "Bordado" "José" 975 975 @(T "UNI" 200)
    Conteo $o @(T "UNI" 200 196 4) @(L 4 500 "UNI" "Hilo de otro tono") "4 con hilo de otro tono."
    Enlace $o; Cita $o (Dia -1 9)
    Api POST "/api/producciones/$($opIds['3-990'])/completar" | Out-Null

    # Cartoonia · OP 1000 · tres procesos
    $o = Orden 2 1000 "NAVIDADGRINCH25" "Liverpool" 50 "Bordado" "José" 1850 1850 @((T "CH" 20), (T "M" 50), (T "G" 30))
    Conteo $o @((T "CH" 20 19 1), (T "M" 50 50 0), (T "G" 30 30 0)) @(L 1 1000 "CH" "Bordado picado") "Una CH con el bordado picado en la manga."
    Enlace $o; Cita $o (Dia -3)
    $o = Orden 2 1000 "NAVIDADGRINCH25" "Liverpool" 51 "Lavandería" "Rica" 1100 800 @((T "CH" 20), (T "M" 50), (T "G" 30))
    Conteo $o @((T "CH" 20 19 1), (T "M" 50 48 2), (T "G" 30 30 0)) @() "Dos M con el tono disparejo."
    Enlace $o
    $o = Orden 2 1000 "NAVIDADGRINCH25" "Liverpool" 52 "Estampado" "María" 650 650 @((T "CH" 20), (T "M" 50), (T "G" 30))
    Enlace $o; Cita $o (Dia 4 14)

    # American Polo · OP 1000 (misma OP que Cartoonia, otra empresa: se vale)
    $o = Orden 1 1000 "SONIC213" "Coppel" 300 "Costura" "José" 3200 3200 @((T "28" 100), (T "30" 150), (T "32" 100))
    Enlace $o
    $o = Orden 1 1000 "SONIC213" "Coppel" 301 "Lavandería" "Raúl" 1400 1400 @((T "28" 100), (T "30" 150), (T "32" 100))

    # Cartoonia · OP 1001 · bordado repartido entre dos proveedores, cada parte con su operación
    $o = Orden 2 1001 "HMNAVIDAD" "Liverpool" 60 "Bordado" "José" 1200 1200 @((T "CH" 30), (T "M" 30))
    Conteo $o @((T "CH" 30 30 0), (T "M" 30 29 1))
    Enlace $o; Cita $o (Dia 4 9)
    $o = Orden 2 1001 "HMNAVIDAD" "Liverpool" 61 "Bordado" "Esteban" 1200 1200 @((T "G" 30), (T "EG" 10))
    Enlace $o; Cita $o (Dia 0 16)

    # Cotton & Print · OP 2310
    $o = Orden 3 2310 "POLOGOB" "Walmart" 7001 "Estampado" "Rica" 1400 1400 @((T "CH" 40), (T "M" 60), (T "G" 40), (T "EG" 20))
    Conteo $o @((T "CH" 40 40 0), (T "M" 60 59 1), (T "G" 40 40 0), (T "EG" 20 20 0))
    Enlace $o; Cita $o (Dia 4 12)

    # MJCO · OP 455 · dos segundas con precio especial
    $o = Orden 4 455 "JOGGER88" "Coppel" 88 "Costura" "María" 2100 1500 @((T "S" 50), (T "M" 80))
    Conteo $o @((T "S" 50 49 1), (T "M" 80 77 3)) @((L 1 1000 "M" "Mancha"), (L 1 0 "M" "Rota")) "Me equivoqué en las M, eran 3 de segunda."
    Enlace $o; Cita $o (Dia 4 16)

    # American Polo · OP 1002 · todo cerrado, lista para completar
    $o = Orden 1 1002 "PIJAMAKIDS" "Suburbia" 302 "Costura" "Raúl" 1500 1000 @((T "CH" 40), (T "M" 40), (T "G" 40))
    Conteo $o @((T "CH" 40 40 0), (T "M" 40 38 2), (T "G" 40 40 0))
    Enlace $o; Cita $o (Dia -2 10)

    Write-Host "Listo.`n" -ForegroundColor Cyan
}

# ---------- Revisiones ----------
$prods = Api GET "/api/producciones"
$ordenes = Api GET "/api/ordenes"
$grinch = ($prods | Where-Object { $_.empresaId -eq 2 -and $_.op -eq 1000 } | Select-Object -First 1).id
$o50 = $ordenes | Where-Object { $_.produccionId -eq $grinch -and $_.np -eq 50 } | Select-Object -First 1
$o61 = $ordenes | Where-Object { $_.np -eq 61 } | Select-Object -First 1

Write-Host "Revisiones:" -ForegroundColor Cyan
Revisar "Hay al menos 7 producciones ($(@($prods).Count))" (@($prods).Count -ge 7)
Revisar "Hay al menos 11 órdenes ($(@($ordenes).Count))" (@($ordenes).Count -ge 11)
Revisar "La operación 50 trae su lote especial en CH" ($o50.lotesSegundas.Count -eq 1 -and $o50.lotesSegundas[0].talla -eq "CH")
Revisar "Las tallas salen en el orden capturado (CH, M, G)" (($o50.tallas.talla -join ",") -eq "CH,M,G")
Revisar "La cita sale como AAAA-MM-DDTHH:mm ($($o61.citaConfirmada))" ($o61.citaConfirmada -match '^\d{4}-\d\d-\d\dT\d\d:\d\d$')

$t1 = (Api POST "/api/ordenes/$($o50.id)/enlace").token
$t2 = (Api POST "/api/ordenes/$($o50.id)/enlace").token
Revisar "Generar enlace dos veces da el mismo ($t1)" ($t1 -and $t1 -eq $t2)

Debe 409 "OP 1000 repetida en Cartoonia" POST "/api/ordenes" @{
    nuevaProduccion = @{ empresaId = 2; op = 1000; modelo = "OTRO"; clienteId = (IdCliente "Coppel") }
    np = 9999; procesoId = $proc["Bordado"]; proveedorId = $prov["José"]; precioCentavos = 100; precioSegundaCentavos = 100
    tallas = @(T "UNI" 1)
}
Debe 409 "Operación 50 repetida en Cartoonia" POST "/api/ordenes" @{
    produccionId = $grinch; np = 50; procesoId = $proc["Bordado"]; proveedorId = $prov["José"]
    precioCentavos = 100; precioSegundaCentavos = 100; tallas = @(T "UNI" 1)
}
Debe 400 "Primeras + segundas pasan de las pedidas" PATCH "/api/ordenes/$($o50.id)" @{ tallas = @((T "CH" 20 20 5), (T "M" 50), (T "G" 30)) }
Debe 400 "Precio especial en M sin segundas en M" PATCH "/api/ordenes/$($o50.id)" @{ lotesSegundas = @(L 2 500 "M" $null) }
Debe 400 "Cita con formato que no es fecha" PUT "/api/ordenes/$($o50.id)/cita" @{ citaConfirmada = "mañana" }
Debe 400 "Orden sin tallas" POST "/api/ordenes" @{
    produccionId = $grinch; np = 9998; procesoId = $proc["Bordado"]; proveedorId = $prov["José"]
    precioCentavos = 100; precioSegundaCentavos = 100; tallas = @()
}
Debe 409 "Cliente 'coppel' repetido (mayúsculas no importan)" POST "/api/clientes" @{ nombre = "coppel" }

$o50b = Api GET "/api/ordenes/$($o50.id)"
Revisar "Lo rechazado no cambió nada (sigue 19/1 en CH)" ($o50b.tallas[0].primeras -eq 19 -and $o50b.tallas[0].segundas -eq 1)

$color = if ($script:mal -eq 0) { "Green" } else { "Red" }
Write-Host "`n$script:bien bien, $script:mal con falla`n" -ForegroundColor $color