#!/bin/bash
# ==============================================================
# Inicializador de SQL Server (contenedor sqlserver-init).
#
# SQL Server NO ejecuta los scripts que se le monten: alguien tiene que
# conectarse al motor y correrlos. Ese alguien es este contenedor, que
# hace su trabajo UNA vez y termina.
#
# Es idempotente: si la base ya existe, no hace nada.
# La contraseña NO está escrita aquí: llega por MSSQL_SA_PASSWORD.
# ==============================================================

set -e

SQLCMD=/opt/mssql-tools18/bin/sqlcmd
SERVER=sqlserver
DB=innovacion_local

echo "[init] ¿Existe ya la base $DB?"
EXISTE=$($SQLCMD -S $SERVER -U sa -P "$MSSQL_SA_PASSWORD" -C -h -1 -W \
  -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM sys.databases WHERE name = '$DB'")

if [ "$EXISTE" = "1" ]; then
    echo "[init] Ya existe. No se hace nada."
    exit 0
fi

echo "[init] Creando la base $DB..."
$SQLCMD -S $SERVER -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "CREATE DATABASE $DB"

echo "[init] Ejecutando innovacion_curricular.sql (25 tablas y los catálogos)..."
$SQLCMD -S $SERVER -U sa -P "$MSSQL_SA_PASSWORD" -C -d $DB -i /scripts/innovacion_curricular.sql

echo "[init] Listo: la base quedó creada y sembrada."
