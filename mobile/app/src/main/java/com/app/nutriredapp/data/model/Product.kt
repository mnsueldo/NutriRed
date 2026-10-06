package com.app.nutriredapp.data.model

import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable

@Serializable
data class Product(
    val id: String? = null,
    val barcode: String,
    val name: String,
    @SerialName("unit_of_measure")
    val unitOfMeasure: String = "unidades",
    val category: String = "General"
)
