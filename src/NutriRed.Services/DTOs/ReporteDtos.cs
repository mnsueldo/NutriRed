using System;
using System.Collections.Generic;

namespace NutriRed.Services.DTOs;

public class ReporteInventarioItemDto
{
    public string Producto { get; set; } = string.Empty;
    public string CodigoEan { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;

    public string NumeroLote { get; set; } = string.Empty;
    public DateTime FechaVencimiento { get; set; }
    public DateTime FechaIngreso { get; set; }
    public string UsuarioIngreso { get; set; } = string.Empty;

    public decimal StockDisponible { get; set; }
    public string UnidadMedida { get; set; } = string.Empty;
    public string Estado { get; set; } = "Óptimo";
}

public class ReporteDonacionItemDto
{
    public int Id { get; set; }
    public string Comprobante { get; set; } = string.Empty;
    public DateTime FechaHora { get; set; }
    public string Donante { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public string TipoDonante { get; set; } = string.Empty;
    public int CantidadItems { get; set; }
    public decimal CantidadTotal { get; set; }
    public string Receptor { get; set; } = string.Empty;
}

public class ReporteEntregaItemDto
{
    public int Id { get; set; } // Identificador para el Comprobante de Despacho
    public string CodigoPaquete { get; set; } = string.Empty;
    public string TipoKit { get; set; } = string.Empty;
    public DateTime FechaEntrega { get; set; }
    public string Familia { get; set; } = string.Empty;
    public string DniTitular { get; set; } = string.Empty;
    public string Voluntario { get; set; } = string.Empty;
    public int TotalAlimentos { get; set; }
}

public class ReporteFamiliaItemDto
{
    public string Familia { get; set; } = string.Empty;
    public string DniTitular { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public int Integrantes { get; set; }
    public int TotalKitsRecibidos { get; set; }
    public DateTime? UltimaEntrega { get; set; }
    public string Estado { get; set; } = "Activa";
}

public class ReporteTrazabilidadItemDto
{
    public string CodigoPaquete { get; set; } = string.Empty;
    public string Alimento { get; set; } = string.Empty;
    public string NumeroLote { get; set; } = string.Empty;
    public string DonanteOrigen { get; set; } = string.Empty;
    public string FamiliaBeneficiaria { get; set; } = string.Empty;
    public DateTime FechaEntrega { get; set; }
}