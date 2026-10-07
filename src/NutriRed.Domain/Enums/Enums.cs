using System.ComponentModel.DataAnnotations;

namespace NutriRed.Domain.Enums;

public enum TipoDonante
{
    Individuo = 1,
    Institucion = 2,
    Anonimo = 3
}

public enum UnidadMedida
{
    [Display(Name = "Unidades")]
    Unidades = 1,

    [Display(Name = "Kilos")]
    Kilos = 2,

    [Display(Name = "Litros")]
    Litros = 3
}

public enum EstadoLote
{
    Disponible = 1,
    Bloqueado = 2,
    Agotado = 3,
    Vencido = 4
}

public enum TipoMovimiento
{
    IngresoDonacion = 1,
    EgresoPaquete = 2,
    BajaPorMerma = 3,
    AjusteManual = 4
}

public enum MotivoMovimiento
{
    DonacionRecibida = 1,
    EntregaPaquete = 2,
    Vencido = 3,
    Deteriorado = 4,
    RoturaEnvase = 5,
    AjusteInventario = 6
}

public enum EstadoFamilia
{
    Activo = 1,
    Suspendido = 2
}

public enum EstadoPaquete
{
    Pendiente = 1,
    Preparado = 2,
    Entregado = 3,
    Cancelado = 4
}

public enum TipoReceptor
{
    Titular = 1,
    TerceroAutorizado = 2
}
