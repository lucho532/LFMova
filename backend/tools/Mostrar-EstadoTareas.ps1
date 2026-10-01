<#
.SYNOPSIS
    Muestra por consola las tareas que no están pendientes y el resumen total.

.DESCRIPTION
    Las tareas pendientes [ ] no se muestran individualmente.

    Estados mostrados:
        [X] Completada        -> Verde
        [!] Bloqueada         -> Rojo
        [~] En progreso       -> Amarillo
        [-] Cancelada         -> Gris oscuro

    Las tareas pendientes sí se contabilizan en el resumen final.

.EXAMPLE
    .\Mostrar-EstadoTareas.ps1
#>

param(
    [string]$RutaTasks = (Join-Path $PSScriptRoot "..\..\docs\sdd\04-execution\tasks.md")
)

if (-not (Test-Path -LiteralPath $RutaTasks)) {
    Write-Host "ERROR: No se encontró tasks.md en:" -ForegroundColor Red
    Write-Host $RutaTasks -ForegroundColor Red
    exit 1
}

$guionLargo = [char]0x2014

$patron = "^## (?:\[(.)\]\s*)?(T\d+[A-Z]?)\s+$guionLargo\s+(.*)$"

$lineas = Get-Content -LiteralPath $RutaTasks -Encoding UTF8

$contadores = @{
    'X' = 0
    '!' = 0
    '~' = 0
    ' ' = 0
    '-' = 0
}

foreach ($linea in $lineas) {

    if ($linea -match $patron) {

        $marca = $matches[1]
        $id = $matches[2]
        $titulo = $matches[3]

        # ============================================================
        # CONTAR TODOS LOS ESTADOS
        # ============================================================

        if ([string]::IsNullOrEmpty($marca)) {

            # Sin marca = pendiente
            $contadores[' ']++

            # Las pendientes NO se muestran.
            continue
        }

        if ($contadores.ContainsKey($marca)) {
            $contadores[$marca]++
        }
        else {

            # Marca desconocida:
            # no mostrar y contabilizar como pendiente.
            $contadores[' ']++
            continue
        }

        # ============================================================
        # MOSTRAR SOLO ESTADOS DISTINTOS DE PENDIENTE
        # ============================================================

        switch ($marca) {

            'X' {
                $color = 'Green'
            }

            '!' {
                $color = 'Red'
            }

            '~' {
                $color = 'Yellow'
            }

            '-' {
                $color = 'DarkGray'
            }

            default {
                continue
            }
        }

        $etiqueta = "[$marca]"

        Write-Host (
            "{0,-3} {1,-8} {2}" -f $etiqueta, $id, $titulo
        ) -ForegroundColor $color
    }
}

# ================================================================
# RESUMEN
# ================================================================

$total = (
    $contadores.Values |
    Measure-Object -Sum
).Sum

Write-Host ""
Write-Host "================ ESTADO DE TAREAS ================" -ForegroundColor White
Write-Host ""

Write-Host ("Completadas:   {0}" -f $contadores['X']) -ForegroundColor Green
Write-Host ("Bloqueadas:    {0}" -f $contadores['!']) -ForegroundColor Red
Write-Host ("En progreso:   {0}" -f $contadores['~']) -ForegroundColor Yellow
Write-Host ("Pendientes:    {0}" -f $contadores[' ']) -ForegroundColor Cyan
Write-Host ("Canceladas:    {0}" -f $contadores['-']) -ForegroundColor DarkGray

Write-Host "-----------------------------------------------" -ForegroundColor DarkGray

Write-Host ("Total:         {0}" -f $total) -ForegroundColor White

