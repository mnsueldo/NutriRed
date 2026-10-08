package com.app.nutriredapp.data.network

import android.util.Log
import com.app.nutriredapp.data.model.Category
import com.app.nutriredapp.data.model.Donation
import com.app.nutriredapp.data.model.Donor
import com.app.nutriredapp.data.model.FoodPackage
import com.app.nutriredapp.data.model.Product
import com.app.nutriredapp.data.model.User
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import kotlinx.serialization.Serializable
import kotlinx.serialization.encodeToString
import kotlinx.serialization.json.Json
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.RequestBody.Companion.toRequestBody
import java.util.concurrent.TimeUnit

object NutriRedApiClient {

    private const val TAG = "NutriRedApiClient"

    // IP local de la PC para probar desde el celular físico conectado a la misma red Wi-Fi:
    var baseUrl: String = "http://192.168.100.42:5137/api/"
    // Nota: Para emulador oficial de Android Studio usar "http://10.0.2.2:5137/api/"

    private val json = Json {
        ignoreUnknownKeys = true
        coerceInputValues = true
        isLenient = true
        encodeDefaults = true
    }

    private val client = OkHttpClient.Builder()
        .connectTimeout(5, TimeUnit.SECONDS)
        .readTimeout(10, TimeUnit.SECONDS)
        .writeTimeout(10, TimeUnit.SECONDS)
        .build()

    private val JSON_MEDIA_TYPE = "application/json; charset=utf-8".toMediaType()

    // ==========================================
    // PRODUCTOS (Catálogo y Escaneo de Código de Barras)
    // ==========================================

    suspend fun getProductByBarcode(barcode: String): Product? = withContext(Dispatchers.IO) {
        val url = "${baseUrl.trimEnd('/')}/productos/barcode/${barcode.trim()}"
        try {
            val request = Request.Builder().url(url).get().build()
            client.newCall(request).execute().use { response ->
                if (!response.isSuccessful) {
                    Log.w(TAG, "getProductByBarcode HTTP ${response.code} para $barcode")
                    return@withContext null
                }
                val bodyStr = response.body?.string() ?: return@withContext null
                return@withContext json.decodeFromString<Product>(bodyStr)
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error de conexión en getProductByBarcode: ${e.message}")
            return@withContext null
        }
    }

    suspend fun getAllProducts(): List<Product> = withContext(Dispatchers.IO) {
        val url = "${baseUrl.trimEnd('/')}/productos"
        try {
            val request = Request.Builder().url(url).get().build()
            client.newCall(request).execute().use { response ->
                if (!response.isSuccessful) {
                    Log.w(TAG, "getAllProducts HTTP ${response.code}")
                    return@withContext emptyList()
                }
                val bodyStr = response.body?.string() ?: return@withContext emptyList()
                return@withContext json.decodeFromString<List<Product>>(bodyStr)
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error de conexión en getAllProducts: ${e.message}")
            return@withContext emptyList()
        }
    }

    suspend fun searchProducts(query: String): List<Product> = withContext(Dispatchers.IO) {
        val url = "${baseUrl.trimEnd('/')}/productos/search?q=${java.net.URLEncoder.encode(query.trim(), "UTF-8")}"
        try {
            val request = Request.Builder().url(url).get().build()
            client.newCall(request).execute().use { response ->
                if (!response.isSuccessful) return@withContext emptyList()
                val bodyStr = response.body?.string() ?: return@withContext emptyList()
                return@withContext json.decodeFromString<List<Product>>(bodyStr)
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error en searchProducts: ${e.message}")
            return@withContext emptyList()
        }
    }

    // ==========================================
    // CATEGORÍAS (Clasificación de Alimentos)
    // ==========================================

    suspend fun getAllCategories(): List<Category> = withContext(Dispatchers.IO) {
        val url = "${baseUrl.trimEnd('/')}/categorias"
        try {
            val request = Request.Builder().url(url).get().build()
            client.newCall(request).execute().use { response ->
                if (!response.isSuccessful) {
                    Log.w(TAG, "getAllCategories HTTP ${response.code}")
                    return@withContext emptyList()
                }
                val bodyStr = response.body?.string() ?: return@withContext emptyList()
                return@withContext json.decodeFromString<List<Category>>(bodyStr)
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error de conexión en getAllCategories: ${e.message}")
            return@withContext emptyList()
        }
    }

    @Serializable
    private data class CrearProductoRequest(
        val barcode: String,
        val name: String,
        val unitOfMeasure: String = "unidades",
        val categoriaId: Int? = null,
        val categoryName: String? = null,
        val category: String? = null
    )

    suspend fun createProduct(product: Product): Product? = withContext(Dispatchers.IO) {
        val url = "${baseUrl.trimEnd('/')}/productos"
        try {
            val reqPayload = CrearProductoRequest(
                barcode = product.barcode,
                name = product.name,
                unitOfMeasure = product.unitOfMeasure,
                categoriaId = product.categoryId,
                categoryName = product.category,
                category = product.category
            )
            val jsonBody = json.encodeToString(reqPayload).toRequestBody(JSON_MEDIA_TYPE)
            val request = Request.Builder().url(url).post(jsonBody).build()
            client.newCall(request).execute().use { response ->
                if (!response.isSuccessful) {
                    Log.w(TAG, "createProduct HTTP ${response.code}")
                    return@withContext null
                }
                val bodyStr = response.body?.string() ?: return@withContext null
                return@withContext json.decodeFromString<Product>(bodyStr)
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error en createProduct: ${e.message}")
            return@withContext null
        }
    }

    // ==========================================
    // DONANTES (Búsqueda y Registro)
    // ==========================================

    suspend fun findDonorByDocument(documentNumber: String): Donor? = withContext(Dispatchers.IO) {
        val url = "${baseUrl.trimEnd('/')}/donantes/buscar?documento=${documentNumber.trim()}"
        try {
            val request = Request.Builder().url(url).get().build()
            client.newCall(request).execute().use { response ->
                if (!response.isSuccessful) return@withContext null
                val bodyStr = response.body?.string() ?: return@withContext null
                return@withContext json.decodeFromString<Donor>(bodyStr)
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error en findDonorByDocument: ${e.message}")
            return@withContext null
        }
    }

    suspend fun getAllDonors(): List<Donor> = withContext(Dispatchers.IO) {
        val url = "${baseUrl.trimEnd('/')}/donantes"
        try {
            val request = Request.Builder().url(url).get().build()
            client.newCall(request).execute().use { response ->
                if (!response.isSuccessful) return@withContext emptyList()
                val bodyStr = response.body?.string() ?: return@withContext emptyList()
                return@withContext json.decodeFromString<List<Donor>>(bodyStr)
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error en getAllDonors: ${e.message}")
            return@withContext emptyList()
        }
    }

    @Serializable
    private data class GuardarDonanteRequest(
        val type: String,
        val documentNumber: String?,
        val name: String,
        val phone: String?,
        val email: String?
    )

    suspend fun saveDonor(donor: Donor): Donor? = withContext(Dispatchers.IO) {
        val url = "${baseUrl.trimEnd('/')}/donantes"
        try {
            val reqPayload = GuardarDonanteRequest(
                type = donor.type.name,
                documentNumber = donor.documentNumber,
                name = donor.name ?: "Donante sin nombre",
                phone = donor.phone,
                email = donor.email
            )
            val jsonBody = json.encodeToString(reqPayload).toRequestBody(JSON_MEDIA_TYPE)
            val request = Request.Builder().url(url).post(jsonBody).build()
            client.newCall(request).execute().use { response ->
                if (!response.isSuccessful) return@withContext null
                val bodyStr = response.body?.string() ?: return@withContext null
                return@withContext json.decodeFromString<Donor>(bodyStr)
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error en saveDonor: ${e.message}")
            return@withContext null
        }
    }

    // ==========================================
    // DONACIONES (Recepción de Alimentos y Stock)
    // ==========================================

    @Serializable
    data class DonacionItemApiRequest(
        val productBarcode: String,
        val productName: String,
        val quantity: Double,
        val unitOfMeasure: String,
        val expirationDate: String,
        val batchNumber: String? = null,
        val categoryId: Int? = null,
        val categoryName: String? = null
    )

    @Serializable
    data class RegistrarDonacionApiRequest(
        val donorType: String? = null,
        val documentNumber: String? = null,
        val donorName: String? = null,
        val phone: String? = null,
        val email: String? = null,
        val volunteerId: String = "VOL-01",
        val volunteerName: String = "Voluntario Recepción",
        val items: List<DonacionItemApiRequest>
    )

    @Serializable
    data class RegistrarDonacionResponse(
        val success: Boolean,
        val id: Int? = null,
        val donationCode: String? = null,
        val timestamp: String? = null,
        val message: String? = null
    )

    suspend fun submitDonation(donation: Donation): Donation? = withContext(Dispatchers.IO) {
        val url = "${baseUrl.trimEnd('/')}/donaciones"
        try {
            val itemsPayload = donation.items.map { item ->
                DonacionItemApiRequest(
                    productBarcode = item.productBarcode,
                    productName = item.productName,
                    quantity = item.quantity,
                    unitOfMeasure = item.unitOfMeasure,
                    expirationDate = item.expirationDate,
                    batchNumber = item.batchNumber,
                    categoryId = item.categoryId,
                    categoryName = item.categoryName
                )
            }

            val reqPayload = RegistrarDonacionApiRequest(
                donorType = donation.donor?.type?.name,
                documentNumber = donation.donor?.documentNumber,
                donorName = donation.donor?.name,
                phone = donation.donor?.phone,
                email = donation.donor?.email,
                volunteerId = donation.volunteerId,
                volunteerName = donation.volunteerName,
                items = itemsPayload
            )

            val jsonBody = json.encodeToString(reqPayload).toRequestBody(JSON_MEDIA_TYPE)
            val request = Request.Builder().url(url).post(jsonBody).build()
            client.newCall(request).execute().use { response ->
                if (!response.isSuccessful) {
                    val err = response.body?.string()
                    Log.w(TAG, "submitDonation falló HTTP ${response.code}: $err")
                    return@withContext null
                }
                val bodyStr = response.body?.string() ?: return@withContext null
                val res = json.decodeFromString<RegistrarDonacionResponse>(bodyStr)
                return@withContext donation.copy(
                    id = res.id,
                    donationCode = res.donationCode ?: donation.donationCode,
                    timestamp = res.timestamp ?: donation.timestamp
                )
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error en submitDonation: ${e.message}")
            return@withContext null
        }
    }

    // ==========================================
    // ENTREGAS (Despacho con Firma Digital)
    // ==========================================

    suspend fun getPreparedPackagesResult(): Result<List<FoodPackage>> = withContext(Dispatchers.IO) {
        val url = "${baseUrl.trimEnd('/')}/entregas/paquetes-preparados"
        try {
            val request = Request.Builder().url(url).get().build()
            client.newCall(request).execute().use { response ->
                if (!response.isSuccessful) {
                    Log.w(TAG, "getPreparedPackages HTTP ${response.code}")
                    return@withContext Result.failure(Exception("HTTP ${response.code}"))
                }
                val bodyStr = response.body?.string() ?: "[]"
                val packages = json.decodeFromString<List<FoodPackage>>(bodyStr)
                return@withContext Result.success(packages)
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error en getPreparedPackages: ${e.message}")
            return@withContext Result.failure(e)
        }
    }

    suspend fun getPreparedPackages(): List<FoodPackage> =
        getPreparedPackagesResult().getOrDefault(emptyList())

    @Serializable
    private data class ConfirmarEntregaRequest(
        val packageId: String?,
        val packageCode: String?,
        val receiverDni: String,
        val receiverName: String,
        val isTitular: Boolean = true,
        val signatureBase64: String? = null,
        val volunteerId: String = "VOL-01"
    )

    @Serializable
    private data class ConfirmarEntregaResponse(
        val success: Boolean,
        val message: String? = null,
        val packageCode: String? = null,
        val deliveredAt: String? = null,
        val receiverDni: String? = null,
        val receiverName: String? = null
    )

    suspend fun confirmDelivery(
        packageId: String,
        packageCode: String,
        receiverDni: String,
        receiverName: String,
        signatureBase64: String,
        volunteerId: String
    ): Boolean = withContext(Dispatchers.IO) {
        val url = "${baseUrl.trimEnd('/')}/entregas/confirmar"
        try {
            val reqPayload = ConfirmarEntregaRequest(
                packageId = packageId,
                packageCode = packageCode,
                receiverDni = receiverDni,
                receiverName = receiverName,
                isTitular = true,
                signatureBase64 = signatureBase64,
                volunteerId = volunteerId
            )
            val jsonBody = json.encodeToString(reqPayload).toRequestBody(JSON_MEDIA_TYPE)
            val request = Request.Builder().url(url).post(jsonBody).build()
            client.newCall(request).execute().use { response ->
                return@withContext response.isSuccessful
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error en confirmDelivery: ${e.message}")
            return@withContext false
        }
    }

    @Serializable
    private data class EntregaFallidaApiRequest(
        val packageCode: String,
        val motivoNoEntrega: String,
        val volunteerId: String = "VOL-01",
        val volunteerName: String = "Voluntario Móvil"
    )

    suspend fun registerFailedDelivery(
        packageCode: String,
        reason: String,
        volunteerName: String = "Voluntario Móvil"
    ): Boolean = withContext(Dispatchers.IO) {
        val url = "${baseUrl.trimEnd('/')}/entregas/fallida"
        try {
            val reqPayload = EntregaFallidaApiRequest(
                packageCode = packageCode.trim(),
                motivoNoEntrega = reason.trim(),
                volunteerName = volunteerName.trim()
            )
            val jsonBody = json.encodeToString(reqPayload).toRequestBody(JSON_MEDIA_TYPE)
            val request = Request.Builder().url(url).post(jsonBody).build()
            client.newCall(request).execute().use { response ->
                if (!response.isSuccessful) {
                    val err = response.body?.string()
                    Log.w(TAG, "registerFailedDelivery HTTP ${response.code}: $err")
                }
                return@withContext response.isSuccessful
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error en registerFailedDelivery: ${e.message}")
            return@withContext false
        }
    }

    // ==========================================
    // AUTENTICACIÓN (Login Voluntario / Admin)
    // ==========================================

    @Serializable
    private data class LoginRequest(
        val email: String,
        val password: String
    )

    @Serializable
    data class LoginResponse(
        val success: Boolean,
        val token: String? = null,
        val message: String? = null,
        val user: User? = null
    )

    suspend fun login(email: String, password: String): Result<User> = withContext(Dispatchers.IO) {
        val url = "${baseUrl.trimEnd('/')}/auth/login"
        try {
            val reqPayload = LoginRequest(email = email.trim(), password = password.trim())
            val jsonBody = json.encodeToString(reqPayload).toRequestBody(JSON_MEDIA_TYPE)
            val request = Request.Builder().url(url).post(jsonBody).build()
            client.newCall(request).execute().use { response ->
                val bodyStr = response.body?.string() ?: ""
                if (response.isSuccessful) {
                    val loginRes = json.decodeFromString<LoginResponse>(bodyStr)
                    val user = loginRes.user ?: User(email = email, fullName = "Voluntario")
                    return@withContext Result.success(user)
                } else {
                    val msg = try {
                        val errObj = json.decodeFromString<LoginResponse>(bodyStr)
                        errObj.message ?: "Credenciales inválidas."
                    } catch (_: Exception) {
                        "Error HTTP ${response.code} al iniciar sesión."
                    }
                    return@withContext Result.failure(Exception(msg))
                }
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error de red en login: ${e.message}")
            return@withContext Result.failure(e)
        }
    }
}
