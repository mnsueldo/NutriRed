package com.app.nutriredapp.data.repository

import android.util.Log
import com.app.nutriredapp.data.model.DonationItem
import com.app.nutriredapp.data.model.FoodPackage
import com.app.nutriredapp.data.model.PackageStatus
import com.app.nutriredapp.data.network.NutriRedApiClient
import kotlinx.coroutines.CoroutineDispatcher
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import java.text.SimpleDateFormat
import java.util.*

class DeliveryRepository(
    private val dispatcher: CoroutineDispatcher = Dispatchers.IO
) {

    private val TAG = "DeliveryRepository"

    // Paquetes preparados precargados en depósito para despacho (contingencia local)
    private val memoryPackages = mutableListOf(
        FoodPackage(
            id = "PKG-001",
            packageCode = "PAQ-2026-0001",
            familyId = "FAM-101",
            familyTitularName = "Carlos Martínez",
            familyTitularDni = "32111222",
            familyMembersCount = 4,
            items = listOf(
                DonationItem(productBarcode = "7791234567890", productName = "Arroz Largo Fino 1kg", quantity = 2.0, unitOfMeasure = "kg", expirationDate = "11/2026", batchNumber = "LOTE-2026-01"),
                DonationItem(productBarcode = "7799876543210", productName = "Fideos Guiseros 500g", quantity = 3.0, unitOfMeasure = "paquetes", expirationDate = "12/2026", batchNumber = "LOTE-2026-02"),
                DonationItem(productBarcode = "7791112223334", productName = "Leche Entera en Polvo 800g", quantity = 2.0, unitOfMeasure = "unidades", expirationDate = "10/2026", batchNumber = "LOTE-2026-03"),
                DonationItem(productBarcode = "7795556667778", productName = "Lentejas Secas 400g", quantity = 2.0, unitOfMeasure = "paquetes", expirationDate = "01/2027", batchNumber = "LOTE-2026-04")
            ),
            status = PackageStatus.PREPARADO
        ),
        FoodPackage(
            id = "PKG-002",
            packageCode = "PAQ-2026-0002",
            familyId = "FAM-102",
            familyTitularName = "María Gómez",
            familyTitularDni = "28999888",
            familyMembersCount = 3,
            items = listOf(
                DonationItem(productBarcode = "7791234567890", productName = "Arroz Largo Fino 1kg", quantity = 1.0, unitOfMeasure = "kg", expirationDate = "11/2026", batchNumber = "LOTE-2026-01"),
                DonationItem(productBarcode = "7799876543210", productName = "Fideos Guiseros 500g", quantity = 2.0, unitOfMeasure = "paquetes", expirationDate = "12/2026", batchNumber = "LOTE-2026-02"),
                DonationItem(productBarcode = "7798889990001", productName = "Aceite de Girasol 900ml", quantity = 1.0, unitOfMeasure = "litros", expirationDate = "03/2027", batchNumber = "LOTE-2026-05"),
                DonationItem(productBarcode = "7793334445556", productName = "Puré de Tomate 520g", quantity = 2.0, unitOfMeasure = "unidades", expirationDate = "02/2027", batchNumber = "LOTE-2026-06")
            ),
            status = PackageStatus.PREPARADO
        ),
        FoodPackage(
            id = "PKG-003",
            packageCode = "PAQ-2026-0003",
            familyId = "FAM-103",
            familyTitularName = "Ana Rodríguez",
            familyTitularDni = "35444555",
            familyMembersCount = 6,
            items = listOf(
                DonationItem(productBarcode = "7791234567890", productName = "Arroz Largo Fino 1kg", quantity = 3.0, unitOfMeasure = "kg", expirationDate = "11/2026", batchNumber = "LOTE-2026-01"),
                DonationItem(productBarcode = "7799876543210", productName = "Fideos Guiseros 500g", quantity = 4.0, unitOfMeasure = "paquetes", expirationDate = "12/2026", batchNumber = "LOTE-2026-02"),
                DonationItem(productBarcode = "7791112223334", productName = "Leche Entera en Polvo 800g", quantity = 3.0, unitOfMeasure = "unidades", expirationDate = "10/2026", batchNumber = "LOTE-2026-03"),
                DonationItem(productBarcode = "7794445556667", productName = "Harina de Trigo 000 1kg", quantity = 2.0, unitOfMeasure = "kg", expirationDate = "04/2027", batchNumber = "LOTE-2026-07")
            ),
            status = PackageStatus.PREPARADO
        )
    )

    suspend fun getPreparedPackages(): List<FoodPackage> = withContext(dispatcher) {
        // 1. Intentar consultar paquetes preparados desde la API central ASP.NET Core
        try {
            val remote = NutriRedApiClient.getPreparedPackages()
            if (remote.isNotEmpty()) {
                remote.forEach { rPkg ->
                    val idx = memoryPackages.indexOfFirst { it.id == rPkg.id || it.packageCode == rPkg.packageCode }
                    if (idx == -1) memoryPackages.add(rPkg) else memoryPackages[idx] = rPkg
                }
                Log.d(TAG, "Paquetes preparados obtenidos de la API .NET: ${remote.size}")
                return@withContext remote
            }
        } catch (e: Exception) {
            Log.w(TAG, "Error obteniendo paquetes desde API: ${e.message}")
        }

        // 2. Fallback a memoria local
        memoryPackages.filter { it.status == PackageStatus.PREPARADO }
    }

    suspend fun getAllPackages(): List<FoodPackage> = withContext(dispatcher) {
        memoryPackages.toList()
    }

    suspend fun savePackage(pkg: FoodPackage): Result<FoodPackage> = withContext(dispatcher) {
        val idx = memoryPackages.indexOfFirst { it.id == pkg.id || it.packageCode == pkg.packageCode }
        if (idx == -1) memoryPackages.add(pkg) else memoryPackages[idx] = pkg
        Result.success(pkg)
    }

    suspend fun getPackageById(id: String): FoodPackage? = withContext(dispatcher) {
        memoryPackages.find { it.id == id || it.packageCode.equals(id, ignoreCase = true) }
    }

    suspend fun confirmDelivery(
        packageId: String,
        receiverDni: String,
        receiverName: String,
        volunteerId: String,
        volunteerName: String
    ): Result<FoodPackage> = withContext(dispatcher) {
        val index = memoryPackages.indexOfFirst { it.id == packageId || it.packageCode.equals(packageId, ignoreCase = true) }
        if (index == -1) {
            return@withContext Result.failure(IllegalArgumentException("El paquete no existe."))
        }

        val current = memoryPackages[index]
        if (current.status == PackageStatus.ENTREGADO) {
            return@withContext Result.failure(IllegalStateException("Este paquete ya ha sido entregado previamente."))
        }

        val nowFormatted = SimpleDateFormat("dd/MM/yyyy HH:mm", Locale.getDefault()).format(Date())

        // 1. Intentar registrar la entrega en la API central ASP.NET Core
        try {
            val success = NutriRedApiClient.confirmDelivery(
                packageId = current.id,
                packageCode = current.packageCode,
                receiverDni = receiverDni.trim(),
                receiverName = receiverName.trim(),
                signatureBase64 = "FIRMA_DIGITAL_TOUCHSCREEN_MOBILE_APP_OK",
                volunteerId = volunteerId
            )
            Log.d(TAG, "Confirmación de entrega en API central .NET: $success")
        } catch (e: Exception) {
            Log.w(TAG, "No se pudo sincronizar entrega con la API: ${e.message}")
        }

        // 2. Actualizar estado local
        val updated = current.copy(
            status = PackageStatus.ENTREGADO,
            receiverDni = receiverDni.trim(),
            receiverName = receiverName.trim(),
            volunteerId = volunteerId,
            volunteerName = volunteerName,
            deliveredAt = nowFormatted,
            hasSignature = true
        )

        memoryPackages[index] = updated
        return@withContext Result.success(updated)
    }
}
