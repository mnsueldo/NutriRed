package com.app.nutriredapp.ui.components

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.rounded.CheckCircle
import androidx.compose.material.icons.rounded.Dns
import androidx.compose.material.icons.rounded.Error
import androidx.compose.material.icons.rounded.Wifi
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.app.nutriredapp.data.network.NutriRedApiClient
import kotlinx.coroutines.launch

@Composable
fun ServerConfigDialog(
    onDismissRequest: () -> Unit
) {
    val coroutineScope = rememberCoroutineScope()
    var inputUrl by remember {
        mutableStateOf(NutriRedApiClient.customBaseUrl ?: NutriRedApiClient.activeBaseUrl ?: "192.168.100.42")
    }
    var isTesting by remember { mutableStateOf(false) }
    var testResult by remember { mutableStateOf<Pair<Boolean, String>?>(null) }

    AlertDialog(
        onDismissRequest = onDismissRequest,
        icon = {
            Icon(
                imageVector = Icons.Rounded.Dns,
                contentDescription = null,
                tint = MaterialTheme.colorScheme.primary,
                modifier = Modifier.size(36.dp)
            )
        },
        title = {
            Text(
                text = "Configuración de Servidor",
                fontWeight = FontWeight.Bold,
                fontSize = 20.sp
            )
        },
        text = {
            Column(
                modifier = Modifier
                    .fillMaxWidth()
                    .verticalScroll(rememberScrollState()),
                verticalArrangement = Arrangement.spacedBy(14.dp)
            ) {
                Text(
                    text = "Configure la dirección IP de su PC para sincronizar con la base de datos central (.NET Web):",
                    style = MaterialTheme.typography.bodyMedium,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )

                OutlinedTextField(
                    value = inputUrl,
                    onValueChange = {
                        inputUrl = it
                        testResult = null
                    },
                    label = { Text("IP o URL del Servidor") },
                    placeholder = { Text("192.168.100.42") },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth()
                )

                Text(
                    text = "Acceso rápido:",
                    style = MaterialTheme.typography.labelMedium,
                    fontWeight = FontWeight.SemiBold
                )

                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    SuggestionChip(
                        onClick = {
                            inputUrl = "192.168.100.42"
                            testResult = null
                        },
                        label = { Text("Wi-Fi PC", fontSize = 12.sp) }
                    )
                    SuggestionChip(
                        onClick = {
                            inputUrl = "10.0.2.2"
                            testResult = null
                        },
                        label = { Text("Emulador", fontSize = 12.sp) }
                    )
                    SuggestionChip(
                        onClick = {
                            inputUrl = "localhost"
                            testResult = null
                        },
                        label = { Text("Localhost", fontSize = 12.sp) }
                    )
                }

                // Botón para probar la conectividad
                OutlinedButton(
                    onClick = {
                        isTesting = true
                        testResult = null
                        coroutineScope.launch {
                            val res = NutriRedApiClient.testConnection(inputUrl)
                            testResult = Pair(res.success, res.message)
                            isTesting = false
                        }
                    },
                    enabled = !isTesting && inputUrl.isNotBlank(),
                    modifier = Modifier.fillMaxWidth()
                ) {
                    if (isTesting) {
                        CircularProgressIndicator(modifier = Modifier.size(18.dp), strokeWidth = 2.dp)
                        Spacer(modifier = Modifier.width(8.dp))
                        Text("Verificando servidor...")
                    } else {
                        Icon(Icons.Rounded.Wifi, contentDescription = null, modifier = Modifier.size(18.dp))
                        Spacer(modifier = Modifier.width(8.dp))
                        Text("Probar Conexión")
                    }
                }

                testResult?.let { (success, message) ->
                    val color = if (success) Color(0xFF1B5E20) else MaterialTheme.colorScheme.error
                    val icon = if (success) Icons.Rounded.CheckCircle else Icons.Rounded.Error

                    Surface(
                        color = if (success) Color(0xFFE8F5E9) else MaterialTheme.colorScheme.errorContainer,
                        shape = MaterialTheme.shapes.small,
                        modifier = Modifier.fillMaxWidth()
                    ) {
                        Row(
                            modifier = Modifier.padding(10.dp),
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Icon(icon, contentDescription = null, tint = color, modifier = Modifier.size(20.dp))
                            Spacer(modifier = Modifier.width(8.dp))
                            Text(
                                text = message,
                                color = color,
                                style = MaterialTheme.typography.bodySmall,
                                fontWeight = FontWeight.Medium
                            )
                        }
                    }
                }
            }
        },
        confirmButton = {
            Button(
                onClick = {
                    NutriRedApiClient.setCustomServerUrl(inputUrl)
                    onDismissRequest()
                }
            ) {
                Text("Guardar y Conectar")
            }
        },
        dismissButton = {
            TextButton(onClick = onDismissRequest) {
                Text("Cerrar")
            }
        }
    )
}
