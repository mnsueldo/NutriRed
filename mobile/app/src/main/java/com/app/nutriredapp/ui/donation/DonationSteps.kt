package com.app.nutriredapp.ui.donation

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.rounded.ArrowForward
import androidx.compose.material.icons.rounded.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.app.nutriredapp.data.model.DonationItem
import com.app.nutriredapp.data.model.DonorType
import com.app.nutriredapp.data.model.Product
import com.app.nutriredapp.ui.components.AccessibleButton
import com.app.nutriredapp.ui.components.AccessibleDropdownField
import com.app.nutriredapp.ui.components.AccessibleTextField

/**
 * PASO 1: Identificación del Donante (Persona, Institución o Anónimo)
 */
@Composable
fun DonorStep(
    uiState: DonationUiState,
    viewModel: DonationViewModel,
    modifier: Modifier = Modifier
) {
    Column(
        modifier = modifier
            .fillMaxWidth()
            .padding(16.dp),
        verticalArrangement = Arrangement.spacedBy(20.dp)
    ) {
        Text(
            text = "Seleccione el tipo de donante:",
            fontSize = 22.sp,
            fontWeight = FontWeight.Bold,
            color = MaterialTheme.colorScheme.onSurface
        )

        // 3 Grandes opciones de tipo de donante (Accesibilidad cognitiva y táctil)
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            DonorTypeCard(
                title = "Persona",
                icon = Icons.Rounded.Person,
                isSelected = uiState.donorType == DonorType.INDIVIDUAL,
                onClick = { viewModel.onDonorTypeChanged(DonorType.INDIVIDUAL) },
                modifier = Modifier.weight(1f)
            )
            DonorTypeCard(
                title = "Empresa",
                icon = Icons.Rounded.Business,
                isSelected = uiState.donorType == DonorType.INSTITUTION,
                onClick = { viewModel.onDonorTypeChanged(DonorType.INSTITUTION) },
                modifier = Modifier.weight(1f)
            )
            DonorTypeCard(
                title = "Anónimo",
                icon = Icons.Rounded.VisibilityOff,
                isSelected = uiState.donorType == DonorType.ANONYMOUS,
                onClick = { viewModel.onDonorTypeChanged(DonorType.ANONYMOUS) },
                modifier = Modifier.weight(1f)
            )
        }

        if (uiState.donorType == DonorType.ANONYMOUS) {
            Surface(
                modifier = Modifier.fillMaxWidth(),
                color = MaterialTheme.colorScheme.secondaryContainer,
                shape = RoundedCornerShape(16.dp)
            ) {
                Row(
                    modifier = Modifier.padding(20.dp),
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.spacedBy(16.dp)
                ) {
                    Icon(
                        imageVector = Icons.Rounded.CheckCircle,
                        contentDescription = null,
                        tint = MaterialTheme.colorScheme.primary,
                        modifier = Modifier.size(36.dp)
                    )
                    Text(
                        text = "Donación Anónima seleccionada.\nNo se requerirán datos personales.",
                        fontSize = 20.sp,
                        fontWeight = FontWeight.Medium,
                        color = MaterialTheme.colorScheme.onSecondaryContainer
                    )
                }
            }
        } else {
            // Formulario para Persona o Institución
            AccessibleTextField(
                value = uiState.documentNumber,
                onValueChange = { viewModel.onDocumentNumberChanged(it) },
                label = if (uiState.donorType == DonorType.INDIVIDUAL) "Número de DNI *" else "Número de CUIT *",
                placeholder = if (uiState.donorType == DonorType.INDIVIDUAL) "Ej. 35123456" else "Ej. 30712345679",
                leadingIcon = Icons.Rounded.Badge,
                keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                isError = uiState.donorError?.contains("documento") == true,
                errorMessage = uiState.donorError
            )

            if (uiState.isDonorExisting) {
                Surface(
                    color = MaterialTheme.colorScheme.primaryContainer,
                    shape = RoundedCornerShape(8.dp),
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Text(
                        text = "✓ Donante registrado anteriormente en la base de datos.",
                        fontSize = 16.sp,
                        fontWeight = FontWeight.Bold,
                        color = MaterialTheme.colorScheme.onPrimaryContainer,
                        modifier = Modifier.padding(12.dp)
                    )
                }
            }

            AccessibleTextField(
                value = uiState.donorName,
                onValueChange = { viewModel.onDonorNameChanged(it) },
                label = if (uiState.donorType == DonorType.INDIVIDUAL) "Nombre y Apellido *" else "Razón Social *",
                placeholder = "Ingrese el nombre completo",
                leadingIcon = Icons.Rounded.AccountBox,
                isError = uiState.donorError?.contains("Nombre") == true,
                errorMessage = uiState.donorError
            )

            AccessibleTextField(
                value = uiState.donorPhone,
                onValueChange = { viewModel.onDonorPhoneChanged(it) },
                label = "Teléfono de Contacto (Opcional)",
                placeholder = "Ej. 11 4567 8900",
                leadingIcon = Icons.Rounded.Phone,
                keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Phone)
            )

            AccessibleTextField(
                value = uiState.donorEmail,
                onValueChange = { viewModel.onDonorEmailChanged(it) },
                label = "Correo Electrónico (Opcional)",
                placeholder = "Ej. contacto@email.com",
                leadingIcon = Icons.Rounded.Email,
                keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Email)
            )
        }

        Spacer(modifier = Modifier.weight(1f, fill = false))

        AccessibleButton(
            text = "CONTINUAR A ALIMENTOS",
            icon = Icons.AutoMirrored.Rounded.ArrowForward,
            iconAfterText = true,
            onClick = { viewModel.validateAndProceedToItems() }
        )
    }
}

@Composable
fun DonorTypeCard(
    title: String,
    icon: androidx.compose.ui.graphics.vector.ImageVector,
    isSelected: Boolean,
    onClick: () -> Unit,
    modifier: Modifier = Modifier
) {
    Surface(
        modifier = modifier
            .height(115.dp)
            .clickable(onClick = onClick),
        shape = RoundedCornerShape(16.dp),
        color = if (isSelected) MaterialTheme.colorScheme.primary else MaterialTheme.colorScheme.surfaceVariant,
        border = BorderStroke(
            width = if (isSelected) 3.dp else 1.dp,
            color = if (isSelected) MaterialTheme.colorScheme.primary else Color.Gray
        )
    ) {
        Column(
            modifier = Modifier.padding(8.dp),
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.Center
        ) {
            Icon(
                imageVector = icon,
                contentDescription = null,
                tint = if (isSelected) MaterialTheme.colorScheme.onPrimary else MaterialTheme.colorScheme.onSurfaceVariant,
                modifier = Modifier.size(38.dp)
            )
            Spacer(modifier = Modifier.height(6.dp))
            Text(
                text = title,
                fontSize = 18.sp,
                fontWeight = FontWeight.Bold,
                color = if (isSelected) MaterialTheme.colorScheme.onPrimary else MaterialTheme.colorScheme.onSurfaceVariant,
                textAlign = TextAlign.Center
            )
        }
    }
}

/**
 * PASO 2: Carga de Alimentos, Registro de Lotes y Política FEFO
 */
@Composable
fun ItemsStep(
    uiState: DonationUiState,
    viewModel: DonationViewModel,
    onOpenScanner: () -> Unit,
    modifier: Modifier = Modifier
) {
    var showCatalogDialog by remember { mutableStateOf(false) }
    val unitOptions = listOf("Unidades", "Kilos", "Litros")

    Column(
        modifier = modifier
            .fillMaxWidth()
            .padding(16.dp),
        verticalArrangement = Arrangement.spacedBy(16.dp)
    ) {
        // Botones de acción rápida: Escaneo con Cámara o Buscar Catálogo
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            Button(
                onClick = onOpenScanner,
                modifier = Modifier
                    .weight(1f)
                    .height(72.dp),
                shape = RoundedCornerShape(16.dp),
                contentPadding = PaddingValues(horizontal = 4.dp, vertical = 8.dp),
                colors = ButtonDefaults.buttonColors(containerColor = MaterialTheme.colorScheme.primary)
            ) {
                Icon(Icons.Rounded.QrCodeScanner, contentDescription = null, modifier = Modifier.size(24.dp))
                Spacer(Modifier.width(6.dp))
                Text("ESCANEAR", fontSize = 16.sp, fontWeight = FontWeight.Bold, maxLines = 1)
            }

            Button(
                onClick = { showCatalogDialog = true },
                modifier = Modifier
                    .weight(1f)
                    .height(72.dp),
                shape = RoundedCornerShape(16.dp),
                contentPadding = PaddingValues(horizontal = 4.dp, vertical = 8.dp),
                colors = ButtonDefaults.buttonColors(containerColor = MaterialTheme.colorScheme.secondary)
            ) {
                Icon(Icons.Rounded.Search, contentDescription = null, modifier = Modifier.size(24.dp))
                Spacer(Modifier.width(6.dp))
                Text("CATÁLOGO", fontSize = 16.sp, fontWeight = FontWeight.Bold, maxLines = 1)
            }
        }

        // Formulario de ingreso de alimento actual
        Card(
            modifier = Modifier.fillMaxWidth(),
            shape = RoundedCornerShape(16.dp),
            colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surfaceVariant)
        ) {
            Column(
                modifier = Modifier.padding(16.dp),
                verticalArrangement = Arrangement.spacedBy(14.dp)
            ) {
                Text(
                    text = "Datos del Alimento:",
                    fontSize = 20.sp,
                    fontWeight = FontWeight.ExtraBold,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )

                if (uiState.currentBarcode.isNotEmpty()) {
                    Surface(
                        shape = RoundedCornerShape(8.dp),
                        color = MaterialTheme.colorScheme.primaryContainer
                    ) {
                        Row(
                            modifier = Modifier.padding(horizontal = 10.dp, vertical = 6.dp),
                            verticalAlignment = Alignment.CenterVertically,
                            horizontalArrangement = Arrangement.spacedBy(6.dp)
                        ) {
                            Icon(
                                imageVector = Icons.Rounded.QrCode,
                                contentDescription = null,
                                tint = MaterialTheme.colorScheme.onPrimaryContainer,
                                modifier = Modifier.size(20.dp)
                            )
                            Text(
                                text = "Código de barras: ${uiState.currentBarcode}",
                                fontSize = 16.sp,
                                fontWeight = FontWeight.Bold,
                                color = MaterialTheme.colorScheme.onPrimaryContainer
                            )
                        }
                    }
                }

                AccessibleTextField(
                    value = uiState.currentProductName,
                    onValueChange = { viewModel.onProductNameChanged(it) },
                    label = "Alimento / Producto *",
                    placeholder = "Ej. Arroz Largo Fino 1kg",
                    leadingIcon = Icons.Rounded.Fastfood
                )

                if (!uiState.isProductFromCatalog) {
                    val categoryOptions = if (uiState.categories.isNotEmpty()) {
                        uiState.categories.map { it.name }
                    } else {
                        listOf("Otros Alimentos / Varios")
                    }

                    AccessibleDropdownField(
                        label = "Categoría del Alimento *",
                        selectedValue = uiState.selectedCategoryName,
                        options = categoryOptions,
                        onOptionSelected = { catName ->
                            val catObj = uiState.categories.find { it.name.trim().equals(catName.trim(), ignoreCase = true) }
                            viewModel.onCategorySelected(catObj?.id, catName)
                        },
                        leadingIcon = Icons.Rounded.Category
                    )
                } else {
                    Surface(
                        shape = RoundedCornerShape(8.dp),
                        color = MaterialTheme.colorScheme.secondaryContainer.copy(alpha = 0.5f)
                    ) {
                        Row(
                            modifier = Modifier.padding(horizontal = 10.dp, vertical = 6.dp),
                            verticalAlignment = Alignment.CenterVertically,
                            horizontalArrangement = Arrangement.spacedBy(6.dp)
                        ) {
                            Icon(
                                imageVector = Icons.Rounded.Category,
                                contentDescription = null,
                                tint = MaterialTheme.colorScheme.onSecondaryContainer,
                                modifier = Modifier.size(18.dp)
                            )
                            Text(
                                text = "Categoría en catálogo: ${uiState.selectedCategoryName}",
                                fontSize = 15.sp,
                                fontWeight = FontWeight.SemiBold,
                                color = MaterialTheme.colorScheme.onSecondaryContainer
                            )
                        }
                    }
                }

                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.spacedBy(12.dp)
                ) {
                    Box(modifier = Modifier.weight(1f)) {
                        AccessibleTextField(
                            value = uiState.currentQuantity,
                            onValueChange = { viewModel.onQuantityChanged(it) },
                            label = "Cantidad *",
                            placeholder = "1",
                            leadingIcon = Icons.Rounded.Numbers,
                            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Decimal)
                        )
                    }

                    Box(modifier = Modifier.weight(1f)) {
                        AccessibleDropdownField(
                            label = "Unidad *",
                            selectedValue = uiState.currentUnitOfMeasure,
                            options = unitOptions,
                            onOptionSelected = { option ->
                                viewModel.onUnitOfMeasureChanged(option)
                            },
                            leadingIcon = Icons.Rounded.Scale
                        )
                    }
                }

                // FEFO: Vencimiento simplificado en Mes y Año con desplegables
                Column(
                    modifier = Modifier.fillMaxWidth(),
                    verticalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    Text(
                        text = "Fecha de Vencimiento *",
                        fontSize = 18.sp,
                        fontWeight = FontWeight.SemiBold,
                        color = MaterialTheme.colorScheme.onSurface
                    )

                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.spacedBy(12.dp)
                    ) {
                        Box(modifier = Modifier.weight(1.3f)) {
                            val selectedMonthText = if (uiState.currentExpirationMonth.isNotEmpty()) {
                                val idx = uiState.currentExpirationMonth.toIntOrNull()?.minus(1)
                                if (idx != null && idx in viewModel.availableMonths.indices) {
                                    viewModel.availableMonths[idx]
                                } else {
                                    uiState.currentExpirationMonth
                                }
                            } else "Mes"

                            AccessibleDropdownField(
                                selectedValue = selectedMonthText,
                                options = viewModel.availableMonths,
                                onOptionSelected = { viewModel.onExpirationMonthChanged(it) }
                            )
                        }

                        Box(modifier = Modifier.weight(1f)) {
                            AccessibleDropdownField(
                                selectedValue = uiState.currentExpirationYear.ifEmpty { "Año" },
                                options = viewModel.getAvailableYears(),
                                onOptionSelected = { viewModel.onExpirationYearChanged(it) }
                            )
                        }
                    }

                    if (uiState.itemErrorMessage != null && 
                        (uiState.itemErrorMessage.contains("vencim") || uiState.itemErrorMessage.contains("vencid"))
                    ) {
                        Row(
                            verticalAlignment = Alignment.CenterVertically,
                            horizontalArrangement = Arrangement.spacedBy(6.dp),
                            modifier = Modifier.padding(start = 4.dp, top = 2.dp)
                        ) {
                            Icon(
                                imageVector = Icons.Rounded.Warning,
                                contentDescription = "Alerta",
                                tint = MaterialTheme.colorScheme.error,
                                modifier = Modifier.size(20.dp)
                            )
                            Text(
                                text = uiState.itemErrorMessage,
                                color = MaterialTheme.colorScheme.error,
                                fontSize = 16.sp,
                                lineHeight = 22.sp,
                                fontWeight = FontWeight.Bold
                            )
                        }
                    }
                }

                // Lote
                AccessibleTextField(
                    value = uiState.currentBatchNumber,
                    onValueChange = { viewModel.onBatchNumberChanged(it) },
                    label = "Número de Lote (Opcional)",
                    placeholder = "Autogenerado si se deja vacío",
                    leadingIcon = Icons.Rounded.QrCode
                )

                AccessibleButton(
                    text = "AGREGAR ALIMENTO A LA LISTA",
                    icon = Icons.Rounded.AddCircle,
                    onClick = { viewModel.addCurrentItemToList() },
                    containerColor = MaterialTheme.colorScheme.tertiary,
                    contentColor = MaterialTheme.colorScheme.onTertiary,
                    minHeight = 60.dp
                )
            }
        }

        // Lista de alimentos cargados en la donación
        Text(
            text = "Alimentos en esta donación (${uiState.items.size}):",
            fontSize = 20.sp,
            fontWeight = FontWeight.Bold
        )

        if (uiState.items.isEmpty()) {
            Box(
                modifier = Modifier
                    .fillMaxWidth()
                    .height(90.dp)
                    .background(Color(0xFFF1F1F1), RoundedCornerShape(12.dp)),
                contentAlignment = Alignment.Center
            ) {
                Text(
                    text = "Aún no has agregado ningún alimento.\nEscanea o ingresa al menos uno.",
                    textAlign = TextAlign.Center,
                    fontSize = 18.sp,
                    color = Color.DarkGray
                )
            }
        } else {
            LazyColumn(
                modifier = Modifier
                    .fillMaxWidth()
                    .heightIn(max = 240.dp),
                verticalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                items(uiState.items) { item ->
                    DonationItemRow(item = item, onDelete = { viewModel.removeItem(item) })
                }
            }
        }

        AccessibleButton(
            text = "CONTINUAR AL RESUMEN",
            icon = Icons.AutoMirrored.Rounded.ArrowForward,
            iconAfterText = true,
            onClick = { viewModel.proceedToSummary() },
            enabled = uiState.items.isNotEmpty()
        )
    }

    if (showCatalogDialog) {
        CatalogSelectionDialog(
            products = uiState.catalogProducts,
            onSelect = { product ->
                viewModel.selectProductFromCatalog(product)
                showCatalogDialog = false
            },
            onDismiss = { showCatalogDialog = false }
        )
    }
}

@Composable
fun DonationItemRow(
    item: DonationItem,
    onDelete: () -> Unit
) {
    Card(
        modifier = Modifier.fillMaxWidth(),
        shape = RoundedCornerShape(12.dp),
        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface),
        border = BorderStroke(1.dp, Color.LightGray)
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(12.dp),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.SpaceBetween
        ) {
            Column(modifier = Modifier.weight(1f)) {
                Text(
                    text = item.productName,
                    fontSize = 18.sp,
                    fontWeight = FontWeight.Bold
                )
                Text(
                    text = "Cantidad: ${item.quantity} ${item.unitOfMeasure}",
                    fontSize = 16.sp,
                    color = MaterialTheme.colorScheme.primary,
                    fontWeight = FontWeight.SemiBold
                )
                Text(
                    text = "Vencimiento FEFO: ${item.expirationDate} • Lote: ${item.batchNumber ?: "S/D"}",
                    fontSize = 15.sp,
                    color = Color.DarkGray
                )
            }
            IconButton(
                onClick = onDelete,
                modifier = Modifier.size(48.dp)
            ) {
                Icon(
                    imageVector = Icons.Rounded.Delete,
                    contentDescription = "Eliminar alimento",
                    tint = MaterialTheme.colorScheme.error,
                    modifier = Modifier.size(30.dp)
                )
            }
        }
    }
}

@Composable
fun CatalogSelectionDialog(
    products: List<Product>,
    onSelect: (Product) -> Unit,
    onDismiss: () -> Unit
) {
    var searchQuery by remember { mutableStateOf("") }
    val filtered = products.filter {
        it.name.contains(searchQuery, ignoreCase = true) ||
        it.category.contains(searchQuery, ignoreCase = true)
    }

    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("Catálogo de Alimentos", fontSize = 24.sp, fontWeight = FontWeight.Bold) },
        text = {
            Column(
                modifier = Modifier.fillMaxWidth(),
                verticalArrangement = Arrangement.spacedBy(12.dp)
            ) {
                AccessibleTextField(
                    value = searchQuery,
                    onValueChange = { searchQuery = it },
                    label = "Buscar alimento",
                    placeholder = "Ej. Arroz, Leche...",
                    leadingIcon = Icons.Rounded.Search
                )

                LazyColumn(
                    modifier = Modifier.height(260.dp),
                    verticalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    items(filtered) { prod ->
                        Surface(
                            modifier = Modifier
                                .fillMaxWidth()
                                .clickable { onSelect(prod) },
                            shape = RoundedCornerShape(8.dp),
                            color = MaterialTheme.colorScheme.surfaceVariant
                        ) {
                            Column(modifier = Modifier.padding(12.dp)) {
                                Text(prod.name, fontSize = 18.sp, fontWeight = FontWeight.Bold)
                                Text(
                                    "Unidad: ${prod.unitOfMeasure} • Categoría: ${prod.category}",
                                    fontSize = 15.sp,
                                    color = Color.DarkGray
                                )
                            }
                        }
                    }
                }
            }
        },
        confirmButton = {},
        dismissButton = {
            TextButton(onClick = onDismiss) {
                Text("CERRAR", fontSize = 18.sp, fontWeight = FontWeight.Bold)
            }
        }
    )
}

/**
 * PASO 3: Resumen y Confirmación Final
 */
@Composable
fun SummaryStep(
    uiState: DonationUiState,
    viewModel: DonationViewModel,
    onBackToItems: () -> Unit,
    modifier: Modifier = Modifier
) {
    Column(
        modifier = modifier
            .fillMaxWidth()
            .padding(16.dp),
        verticalArrangement = Arrangement.spacedBy(18.dp)
    ) {
        Text(
            text = "Resumen de la Donación",
            fontSize = 26.sp,
            fontWeight = FontWeight.ExtraBold,
            color = MaterialTheme.colorScheme.onSurface
        )

        // Tarjeta del Donante
        Card(
            modifier = Modifier.fillMaxWidth(),
            shape = RoundedCornerShape(16.dp),
            colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.primaryContainer)
        ) {
            Column(modifier = Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(6.dp)) {
                Text(
                    text = "DATOS DEL DONANTE",
                    fontSize = 16.sp,
                    fontWeight = FontWeight.ExtraBold,
                    color = MaterialTheme.colorScheme.onPrimaryContainer
                )
                Text(
                    text = if (uiState.donorType == DonorType.ANONYMOUS) "Donación Anónima" else uiState.donorName,
                    fontSize = 22.sp,
                    fontWeight = FontWeight.Bold
                )
                if (uiState.donorType != DonorType.ANONYMOUS) {
                    Text(
                        text = "Documento/CUIT: ${uiState.documentNumber}",
                        fontSize = 18.sp
                    )
                    if (uiState.donorPhone.isNotEmpty()) {
                        Text(text = "Tel: ${uiState.donorPhone}", fontSize = 16.sp)
                    }
                }
            }
        }

        // Tarjeta de Alimentos
        Card(
            modifier = Modifier.fillMaxWidth(),
            shape = RoundedCornerShape(16.dp),
            colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surfaceVariant)
        ) {
            Column(modifier = Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(10.dp)) {
                Text(
                    text = "ALIMENTOS A INGRESAR (${uiState.items.size} ítems)",
                    fontSize = 16.sp,
                    fontWeight = FontWeight.ExtraBold,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )

                uiState.items.forEachIndexed { index, item ->
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween
                    ) {
                        Text(
                            text = "${index + 1}. ${item.productName}",
                            fontSize = 18.sp,
                            fontWeight = FontWeight.SemiBold,
                            modifier = Modifier.weight(1f)
                        )
                        Text(
                            text = "${item.quantity} ${item.unitOfMeasure}",
                            fontSize = 18.sp,
                            fontWeight = FontWeight.Bold,
                            color = MaterialTheme.colorScheme.primary
                        )
                    }
                    Text(
                        text = "   Vence: ${item.expirationDate} | Lote: ${item.batchNumber ?: "-"}",
                        fontSize = 15.sp,
                        color = Color.DarkGray
                    )
                    HorizontalDivider(modifier = Modifier.padding(vertical = 4.dp))
                }
            }
        }

        if (uiState.globalError != null) {
            Text(
                text = uiState.globalError,
                color = MaterialTheme.colorScheme.error,
                fontSize = 18.sp,
                fontWeight = FontWeight.Bold
            )
        }

        Spacer(modifier = Modifier.weight(1f, fill = false))

        AccessibleButton(
            text = "CONFIRMAR Y GUARDAR DONACIÓN",
            icon = Icons.Rounded.CheckCircle,
            onClick = { viewModel.confirmDonation() },
            enabled = !uiState.isSubmitting
        )

        AccessibleButton(
            text = "MODIFICAR ALIMENTOS",
            icon = Icons.Rounded.Edit,
            onClick = onBackToItems,
            containerColor = Color.LightGray,
            contentColor = Color.Black,
            minHeight = 56.dp
        )
    }
}

/**
 * PASO 4: Comprobante Digital en Pantalla
 */
@Composable
fun ReceiptStep(
    uiState: DonationUiState,
    onNewDonation: () -> Unit,
    onBackToHome: () -> Unit,
    modifier: Modifier = Modifier
) {
    val donation = uiState.completedDonation

    Column(
        modifier = modifier
            .fillMaxWidth()
            .padding(16.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.spacedBy(18.dp)
    ) {
        Surface(
            shape = CircleShape,
            color = MaterialTheme.colorScheme.primary,
            modifier = Modifier.size(80.dp)
        ) {
            Box(contentAlignment = Alignment.Center) {
                Icon(
                    imageVector = Icons.Rounded.Check,
                    contentDescription = null,
                    tint = Color.White,
                    modifier = Modifier.size(50.dp)
                )
            }
        }

        Text(
            text = "¡Donación Registrada!",
            fontSize = 30.sp,
            fontWeight = FontWeight.Black,
            color = MaterialTheme.colorScheme.primary
        )

        Text(
            text = "El inventario ha sido actualizado bajo política FEFO.",
            fontSize = 18.sp,
            textAlign = TextAlign.Center,
            fontWeight = FontWeight.Medium
        )

        // Comprobante
        Card(
            modifier = Modifier.fillMaxWidth(),
            shape = RoundedCornerShape(16.dp),
            colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surfaceVariant),
            border = BorderStroke(2.dp, MaterialTheme.colorScheme.primary)
        ) {
            Column(
                modifier = Modifier.padding(20.dp),
                verticalArrangement = Arrangement.spacedBy(10.dp)
            ) {
                Text(
                    text = "COMPROBANTE DE RECEPCIÓN",
                    fontSize = 16.sp,
                    fontWeight = FontWeight.Black,
                    color = MaterialTheme.colorScheme.primary
                )

                Text(
                    text = donation?.donationCode ?: "DON-2026-00045",
                    fontSize = 32.sp,
                    fontWeight = FontWeight.ExtraBold,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )

                HorizontalDivider()

                Text(
                    text = "Fecha y hora: ${donation?.timestamp ?: "Ahora"}",
                    fontSize = 18.sp
                )
                Text(
                    text = "Voluntario: ${donation?.volunteerName ?: "Voluntario Receptor"}",
                    fontSize = 18.sp
                )
                Text(
                    text = "Donante: ${donation?.donor?.name ?: "Anónimo"}",
                    fontSize = 18.sp,
                    fontWeight = FontWeight.Bold
                )

                HorizontalDivider()

                Text(
                    text = "Total de alimentos recibidos: ${donation?.items?.size ?: 0}",
                    fontSize = 18.sp,
                    fontWeight = FontWeight.Bold
                )
            }
        }

        Spacer(modifier = Modifier.weight(1f, fill = false))

        AccessibleButton(
            text = "REGISTRAR OTRA DONACIÓN",
            icon = Icons.Rounded.Add,
            onClick = onNewDonation
        )

        AccessibleButton(
            text = "VOLVER AL MENÚ PRINCIPAL",
            icon = Icons.Rounded.Home,
            onClick = onBackToHome,
            containerColor = MaterialTheme.colorScheme.secondary,
            minHeight = 60.dp
        )
    }
}
