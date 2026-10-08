package com.app.nutriredapp.data.repository

import android.util.Log
import com.app.nutriredapp.data.model.Product
import com.app.nutriredapp.data.network.NutriRedApiClient
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext

class ProductRepository {

    private val TAG = "ProductRepository"

    // Catálogo precargado de alimentos de primera necesidad para bancos de alimentos (contingencia local)
    private val memoryCatalog = mutableListOf(
        Product(
            id = "PROD-001",
            barcode = "7791234567890",
            name = "Arroz Largo Fino 1kg",
            unitOfMeasure = "kg",
            category = "Cereales y Granos"
        ),
        Product(
            id = "PROD-002",
            barcode = "7799876543210",
            name = "Fideos Guiseros 500g",
            unitOfMeasure = "paquetes",
            category = "Pastas Secas"
        ),
        Product(
            id = "PROD-003",
            barcode = "7791112223334",
            name = "Leche Entera en Polvo 800g",
            unitOfMeasure = "unidades",
            category = "Lácteos"
        ),
        Product(
            id = "PROD-004",
            barcode = "7795556667778",
            name = "Lentejas Secas 400g",
            unitOfMeasure = "paquetes",
            category = "Legumbres"
        ),
        Product(
            id = "PROD-005",
            barcode = "7798889990001",
            name = "Aceite de Girasol 900ml",
            unitOfMeasure = "litros",
            category = "Aceites y Grasas"
        ),
        Product(
            id = "PROD-006",
            barcode = "7793334445556",
            name = "Puré de Tomate 520g",
            unitOfMeasure = "unidades",
            category = "Conservas y Enlatados"
        ),
        Product(
            id = "PROD-007",
            barcode = "7794445556667",
            name = "Harina de Trigo 000 1kg",
            unitOfMeasure = "kg",
            category = "Harinas"
        )
    )

    suspend fun findProductByBarcode(barcode: String): Product? = withContext(Dispatchers.IO) {
        val cleanBarcode = barcode.trim()

        // 1. Intentar consultar la API central ASP.NET Core
        try {
            val remote = NutriRedApiClient.getProductByBarcode(cleanBarcode)
            if (remote != null) {
                val idx = memoryCatalog.indexOfFirst { it.barcode == cleanBarcode }
                if (idx != -1) {
                    memoryCatalog[idx] = remote
                } else {
                    memoryCatalog.add(remote)
                }
                Log.d(TAG, "Producto obtenido de ASP.NET Core API: ${remote.name}")
                return@withContext remote
            }
        } catch (e: Exception) {
            Log.w(TAG, "Fallo al consultar API remota: ${e.message}")
        }

        // 2. Fallback a catálogo en memoria
        val local = memoryCatalog.find { it.barcode == cleanBarcode }
        if (local != null) {
            Log.d(TAG, "Producto obtenido de caché local: ${local.name}")
            return@withContext local
        }

        return@withContext null
    }

    suspend fun searchProducts(query: String): List<Product> = withContext(Dispatchers.IO) {
        val q = query.trim().lowercase()

        // 1. Intentar buscar en la API central
        try {
            val remoteResults = NutriRedApiClient.searchProducts(query)
            if (remoteResults.isNotEmpty()) {
                remoteResults.forEach { r ->
                    if (memoryCatalog.none { it.barcode == r.barcode }) {
                        memoryCatalog.add(r)
                    }
                }
                return@withContext remoteResults
            }
        } catch (e: Exception) {
            Log.w(TAG, "Fallo búsqueda en API: ${e.message}")
        }

        // 2. Fallback a búsqueda local
        if (q.isEmpty()) return@withContext memoryCatalog.toList()
        memoryCatalog.filter {
            it.name.lowercase().contains(q) ||
            it.barcode.contains(q) ||
            it.category.lowercase().contains(q)
        }
    }

    suspend fun addProduct(product: Product): Product = withContext(Dispatchers.IO) {
        val productWithId = if (product.id == null) {
            product.copy(id = "PROD-${System.currentTimeMillis() % 100000}")
        } else {
            product
        }

        // 1. Intentar registrar en API central ASP.NET Core
        try {
            val created = NutriRedApiClient.createProduct(productWithId)
            if (created != null) {
                memoryCatalog.add(created)
                return@withContext created
            }
        } catch (e: Exception) {
            Log.w(TAG, "No se pudo sincronizar producto con API: ${e.message}")
        }

        // 2. Guardado local de contingencia
        memoryCatalog.add(productWithId)
        return@withContext productWithId
    }

    suspend fun getAllProducts(): List<Product> = withContext(Dispatchers.IO) {
        try {
            val remoteList = NutriRedApiClient.getAllProducts()
            if (remoteList.isNotEmpty()) {
                remoteList.forEach { r ->
                    val idx = memoryCatalog.indexOfFirst { it.barcode == r.barcode }
                    if (idx != -1) {
                        memoryCatalog[idx] = r
                    } else {
                        memoryCatalog.add(r)
                    }
                }
                return@withContext memoryCatalog.toList()
            }
        } catch (e: Exception) {
            Log.w(TAG, "Error obteniendo catálogo de API: ${e.message}")
        }
        memoryCatalog.toList()
    }
}
