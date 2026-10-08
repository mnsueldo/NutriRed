package com.app.nutriredapp.data.model

import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable
import kotlinx.serialization.Transient

@Serializable
data class DonationItem(
    val id: String? = null,
    @SerialName("product_id")
    val productId: String? = null,
    @SerialName("product_barcode")
    val productBarcode: String,
    @SerialName("product_name")
    val productName: String,
    val quantity: Double,
    @SerialName("unit_of_measure")
    val unitOfMeasure: String,
    @SerialName("expiration_date")
    val expirationDate: String, // Formato MM/AAAA
    @SerialName("batch_number")
    val batchNumber: String? = null,
    @SerialName("category_id")
    val categoryId: Int? = null,
    @SerialName("category_name")
    val categoryName: String? = null
)

@Serializable
data class Donation(
    val id: Int? = null,
    @Transient val barcode: String = "",
    @Transient val name: String = "",
    @Transient val quantity: Int = 0,
    val timestamp: String? = null,
    @SerialName("donation_code")
    val donationCode: String = "",
    @SerialName("donor_id")
    val donorId: String? = null,
    @Transient val donor: Donor? = null,
    @SerialName("volunteer_id")
    val volunteerId: String = "VOL-01",
    @SerialName("volunteer_name")
    val volunteerName: String = "Voluntario de Recepción",
    val items: List<DonationItem> = emptyList()
)
