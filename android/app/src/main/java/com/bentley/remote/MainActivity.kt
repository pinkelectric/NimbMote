package com.bentley.remote

import android.Manifest
import android.app.StatusBarManager
import android.content.ComponentName
import android.content.Intent
import android.content.Context
import android.graphics.drawable.Icon
import android.net.Uri
import android.graphics.BitmapFactory
import android.os.Build
import android.os.Bundle
import android.os.SystemClock
import android.provider.Settings
import androidx.activity.ComponentActivity
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.compose.setContent
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.Image
import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.background
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.aspectRatio
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Bedtime
import androidx.compose.material.icons.filled.Lock
import androidx.compose.material.icons.filled.MoreVert
import androidx.compose.material.icons.filled.Pause
import androidx.compose.material.icons.filled.PlayArrow
import androidx.compose.material.icons.filled.PowerSettingsNew
import androidx.compose.material.icons.filled.Refresh
import androidx.compose.material.icons.filled.RestartAlt
import androidx.compose.material.icons.filled.SettingsEthernet
import androidx.compose.material.icons.filled.SkipNext
import androidx.compose.material.icons.filled.SkipPrevious
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Slider
import androidx.compose.material3.Switch
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableFloatStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.ImageBitmap
import androidx.compose.ui.graphics.asImageBitmap
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.core.content.FileProvider
import com.bentley.remote.data.RemoteRepository
import com.bentley.remote.diagnostics.ConnectionDiagnostics
import com.bentley.remote.media.TimelinePredictor
import com.bentley.remote.model.AppUiState
import com.bentley.remote.service.BentleyRemoteService
import com.bentley.remote.service.DeskoraQuickTileService
import kotlinx.coroutines.delay
import java.text.NumberFormat
import java.io.File
import java.util.Locale

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        RemoteRepository.initialize(this)
        BentleyRemoteService.start(this)

        setContent {
            val notificationPermission = rememberLauncherForActivityResult(
                ActivityResultContracts.RequestPermission(),
            ) { }
            LaunchedEffect(Unit) {
                if (Build.VERSION.SDK_INT >= 33) {
                    notificationPermission.launch(Manifest.permission.POST_NOTIFICATIONS)
                }
            }
            MaterialTheme(
                colorScheme = if (isSystemInDarkTheme()) darkColorScheme() else lightColorScheme(),
            ) {
                BentleyRemoteScreen()
            }
        }
    }

    override fun onResume() {
        super.onResume()
        // Reassert the local transport when the user returns after Android has reclaimed it.
        BentleyRemoteService.start(this)
        RemoteRepository.requestDesktopPreview()
    }
}

private data class PowerUiAction(
    val action: String,
    val label: Int,
    val confirmation: Int?,
)

private val primaryPowerActions = listOf(
    PowerUiAction("shutdown", R.string.shutdown, R.string.shutdown_confirmation),
    PowerUiAction("restart", R.string.restart, R.string.restart_confirmation),
)
private val secondaryPowerActions = listOf(
    PowerUiAction("sleep", R.string.sleep, R.string.sleep_confirmation),
    PowerUiAction("lock", R.string.lock, null),
)

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun BentleyRemoteScreen() {
    val context = LocalContext.current
    val state by RemoteRepository.state.collectAsStateWithLifecycle()
    var reverseHost by remember(state.reverseHost) { mutableStateOf(state.reverseHost) }
    var reverseEnabled by remember(state.reverseEnabled) { mutableStateOf(state.reverseEnabled) }
    var showConnectionSettings by remember { mutableStateOf(false) }
    var showDiagnostics by remember { mutableStateOf(false) }

    LaunchedEffect(state.connected) {
        if (state.connected) RemoteRepository.requestDesktopPreview()
    }

    Scaffold { padding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
                .verticalScroll(rememberScrollState())
                .padding(18.dp),
            verticalArrangement = Arrangement.spacedBy(14.dp),
        ) {
            if (state.pairedComputer == null) {
                PairingCard(
                    state = state,
                    onOpenConnectionSettings = { showConnectionSettings = true },
                )
            } else {
                ComputerCard(
                    state = state,
                    onRefreshDesktop = RemoteRepository::requestDesktopPreview,
                    onOpenConnectionSettings = { showConnectionSettings = true },
                    onShowDiagnostics = { showDiagnostics = true },
                )
                TestPackageCard(state = state)
                MediaCard(state = state)
                VolumeCard(state = state)
                PairingCard(
                    state = state,
                    onOpenConnectionSettings = { showConnectionSettings = true },
                )
                QuickTileCard(context)
                OutlinedButton(
                    onClick = {
                        context.startActivity(Intent(Settings.ACTION_IGNORE_BATTERY_OPTIMIZATION_SETTINGS))
                    },
                    modifier = Modifier.fillMaxWidth(),
                ) { Text(stringResource(R.string.battery_settings)) }
            }
        }
    }

    if (showConnectionSettings) {
        ConnectionSettingsDialog(
            reverseEnabled = reverseEnabled,
            reverseHost = reverseHost,
            onReverseEnabledChanged = { reverseEnabled = it },
            onReverseHostChanged = { reverseHost = it },
            onApply = { RemoteRepository.updateReverse(context, reverseEnabled, reverseHost) },
            onDismiss = { showConnectionSettings = false },
        )
    }
    if (showDiagnostics) {
        ConnectionDiagnosticsDialog(onDismiss = { showDiagnostics = false })
    }
}

@Composable
private fun QuickTileCard(context: Context) {
    Card {
        Column(
            modifier = Modifier.padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(8.dp),
        ) {
            Text(stringResource(R.string.quick_tile_title), fontWeight = FontWeight.SemiBold)
            Text(stringResource(R.string.quick_tile_description))
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
                OutlinedButton(
                    onClick = { requestQuickTile(context) },
                    modifier = Modifier.fillMaxWidth(),
                ) { Text(stringResource(R.string.add_quick_tile)) }
            } else {
                Text(stringResource(R.string.quick_tile_legacy_hint))
            }
        }
    }
}

private fun requestQuickTile(context: Context) {
    if (Build.VERSION.SDK_INT < Build.VERSION_CODES.TIRAMISU) return
    val statusBarManager = context.getSystemService(StatusBarManager::class.java) ?: return
    statusBarManager.requestAddTileService(
        ComponentName(context, DeskoraQuickTileService::class.java),
        context.getString(R.string.quick_tile_label),
        Icon.createWithResource(context, R.drawable.ic_deskora_quick_tile),
        context.mainExecutor,
    ) { }
}

@Composable
private fun TestPackageCard(state: AppUiState) {
    val testPackage = state.testPackage
    if (testPackage.status in setOf("idle", "unavailable")) return
    val context = LocalContext.current
    Card {
        Column(
            modifier = Modifier.padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(8.dp),
        ) {
            Text(stringResource(R.string.test_package), fontWeight = FontWeight.SemiBold)
            Text(testPackage.label ?: stringResource(R.string.test_package_unavailable))
            when (testPackage.status) {
                "available" -> Button(
                    onClick = RemoteRepository::downloadTestPackage,
                    enabled = state.connected,
                    modifier = Modifier.fillMaxWidth(),
                ) { Text(stringResource(R.string.download_test_package)) }
                "waiting", "downloading" -> Text(stringResource(R.string.test_package_downloading))
                "ready" -> Button(
                    onClick = { testPackage.localPath?.let { installTestPackage(context, it) } },
                    modifier = Modifier.fillMaxWidth(),
                ) { Text(stringResource(R.string.install_test_package)) }
                "failed" -> {
                    Text(stringResource(R.string.test_package_failed))
                    OutlinedButton(onClick = RemoteRepository::downloadTestPackage, modifier = Modifier.fillMaxWidth()) {
                        Text(stringResource(R.string.try_again))
                    }
                }
            }
        }
    }
}

private fun installTestPackage(context: Context, path: String) {
    if (Build.VERSION.SDK_INT >= 26 && !context.packageManager.canRequestPackageInstalls()) {
        context.startActivity(Intent(Settings.ACTION_MANAGE_UNKNOWN_APP_SOURCES, Uri.parse("package:${context.packageName}")))
        return
    }
    val file = File(path)
    if (!file.isFile) return
    val uri = FileProvider.getUriForFile(context, "${context.packageName}.files", file)
    context.startActivity(
        Intent(Intent.ACTION_VIEW)
            .setDataAndType(uri, "application/vnd.android.package-archive")
            .addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION or Intent.FLAG_ACTIVITY_NEW_TASK),
    )
}

@Composable
private fun ComputerCard(
    state: AppUiState,
    onRefreshDesktop: () -> Unit,
    onOpenConnectionSettings: () -> Unit,
    onShowDiagnostics: () -> Unit,
) {
    var pendingAction by remember { mutableStateOf<PowerUiAction?>(null) }
    var menuExpanded by remember { mutableStateOf(false) }
    val preview = state.desktopPreview

    Card {
        Box(
            modifier = Modifier
                .fillMaxWidth()
                .aspectRatio(16f / 9f),
        ) {
            DesktopPreview(
                bytes = preview.image,
                status = preview.status,
                modifier = Modifier.matchParentSize(),
            )
            Box(
                modifier = Modifier
                    .matchParentSize()
                    .background(
                        Brush.verticalGradient(
                            0f to Color.Black.copy(alpha = 0.58f),
                            0.42f to Color.Black.copy(alpha = 0.08f),
                            1f to Color.Black.copy(alpha = 0.68f),
                        ),
                    ),
            )
            Column(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(16.dp),
                verticalArrangement = Arrangement.SpaceBetween,
            ) {
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Column(modifier = Modifier.weight(1f)) {
                        Text(
                            text = state.pairedComputer ?: stringResource(R.string.computer),
                            color = Color.White,
                            fontWeight = FontWeight.SemiBold,
                        )
                        Text(
                            text = stringResource(
                                if (state.connected) R.string.connected else R.string.not_connected,
                            ),
                            color = Color.White.copy(alpha = 0.86f),
                            style = MaterialTheme.typography.bodySmall,
                        )
                    }
                    IconButton(onClick = onRefreshDesktop, enabled = state.connected) {
                        Icon(
                            imageVector = Icons.Default.Refresh,
                            contentDescription = stringResource(R.string.refresh_desktop),
                            tint = Color.White,
                        )
                    }
                    Box {
                        IconButton(onClick = { menuExpanded = true }) {
                            Icon(
                                imageVector = Icons.Default.MoreVert,
                                contentDescription = stringResource(R.string.more),
                                tint = Color.White,
                            )
                        }
                        DropdownMenu(
                            expanded = menuExpanded,
                            onDismissRequest = { menuExpanded = false },
                        ) {
                            secondaryPowerActions.forEach { action ->
                                DropdownMenuItem(
                                    text = { Text(stringResource(action.label)) },
                                    leadingIcon = {
                                        Icon(
                                            imageVector = if (action.action == "sleep") {
                                                Icons.Default.Bedtime
                                            } else {
                                                Icons.Default.Lock
                                            },
                                            contentDescription = null,
                                        )
                                    },
                                    enabled = state.connected,
                                    onClick = {
                                        menuExpanded = false
                                        if (action.confirmation == null) {
                                            RemoteRepository.systemAction(action.action)
                                        } else {
                                            pendingAction = action
                                        }
                                    },
                                )
                            }
                            DropdownMenuItem(
                                text = { Text(stringResource(R.string.connection_settings)) },
                                leadingIcon = {
                                    Icon(
                                        imageVector = Icons.Default.SettingsEthernet,
                                        contentDescription = null,
                                    )
                                },
                                onClick = {
                                    menuExpanded = false
                                    onOpenConnectionSettings()
                                },
                            )
                            DropdownMenuItem(
                                text = { Text(stringResource(R.string.connection_diagnostics)) },
                                onClick = {
                                    menuExpanded = false
                                    onShowDiagnostics()
                                },
                            )
                        }
                    }
                }

                Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.spacedBy(10.dp),
                    ) {
                        primaryPowerActions.forEach { action ->
                            Button(
                                onClick = { pendingAction = action },
                                enabled = state.connected,
                                modifier = Modifier.weight(1f),
                                colors = ButtonDefaults.buttonColors(
                                    containerColor = Color.White.copy(alpha = 0.18f),
                                    contentColor = Color.White,
                                    disabledContainerColor = Color.Black.copy(alpha = 0.16f),
                                    disabledContentColor = Color.White.copy(alpha = 0.48f),
                                ),
                                border = BorderStroke(1.dp, Color.White.copy(alpha = 0.48f)),
                                contentPadding = PaddingValues(horizontal = 8.dp, vertical = 8.dp),
                            ) {
                                Icon(
                                    imageVector = if (action.action == "shutdown") {
                                        Icons.Default.PowerSettingsNew
                                    } else {
                                        Icons.Default.RestartAlt
                                    },
                                    contentDescription = null,
                                    modifier = Modifier.size(22.dp),
                                )
                                Spacer(modifier = Modifier.width(6.dp))
                                Text(
                                    text = stringResource(action.label),
                                    fontSize = 15.sp,
                                    maxLines = 1,
                                    softWrap = false,
                                )
                            }
                        }
                    }
                    state.commandResult?.let { result ->
                        Text(
                            text = result,
                            color = Color.White.copy(alpha = 0.9f),
                            style = MaterialTheme.typography.bodySmall,
                        )
                    }
                }
            }
        }
    }

    pendingAction?.let { action ->
        AlertDialog(
            onDismissRequest = { pendingAction = null },
            title = { Text(stringResource(action.label)) },
            text = { Text(stringResource(action.confirmation!!)) },
            confirmButton = {
                TextButton(
                    onClick = {
                        pendingAction = null
                        RemoteRepository.systemAction(action.action)
                    },
                ) { Text(stringResource(R.string.continue_action)) }
            },
            dismissButton = {
                TextButton(onClick = { pendingAction = null }) {
                    Text(stringResource(R.string.cancel))
                }
            },
        )
    }
}

@Composable
private fun DesktopPreview(bytes: ByteArray?, status: String, modifier: Modifier = Modifier) {
    val bitmap: ImageBitmap? = remember(bytes) {
        bytes?.let { imageBytes ->
            BitmapFactory.decodeByteArray(imageBytes, 0, imageBytes.size)?.asImageBitmap()
        }
    }
    val previewModifier = modifier
        .background(
            color = MaterialTheme.colorScheme.surfaceVariant,
        )

    if (bitmap != null) {
        Image(
            bitmap = bitmap,
            contentDescription = stringResource(R.string.desktop_preview),
            contentScale = ContentScale.Crop,
            modifier = previewModifier,
        )
    } else {
        Box(modifier = previewModifier, contentAlignment = Alignment.Center) {
            Text(
                text = stringResource(
                    if (status == "loading") R.string.desktop_loading else R.string.desktop_unavailable,
                ),
                color = MaterialTheme.colorScheme.onSurfaceVariant,
            )
        }
    }
}

@Composable
private fun MediaCard(state: AppUiState) {
    val media = state.media
    Card {
        Column(
            modifier = Modifier.padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp),
        ) {
            MediaDetails(mediaArtwork = media.artwork, state = state)
            MediaTimeline(state = state)
            MediaControls(state = state)
        }
    }
}

@Composable
private fun MediaDetails(mediaArtwork: ByteArray?, state: AppUiState) {
    val media = state.media
    val artwork = remember(mediaArtwork) {
        mediaArtwork?.let { imageBytes ->
            BitmapFactory.decodeByteArray(imageBytes, 0, imageBytes.size)?.asImageBitmap()
        }
    }
    Row(
        horizontalArrangement = Arrangement.spacedBy(14.dp),
        verticalAlignment = Alignment.CenterVertically,
    ) {
        if (artwork != null) {
            Image(
                bitmap = artwork,
                contentDescription = stringResource(R.string.artwork),
                contentScale = ContentScale.Crop,
                modifier = Modifier.size(92.dp),
            )
        } else {
            Box(
                modifier = Modifier
                    .size(92.dp)
                    .background(
                        color = MaterialTheme.colorScheme.secondaryContainer,
                        shape = RoundedCornerShape(10.dp),
                    ),
                contentAlignment = Alignment.Center,
            ) {
                Icon(
                    imageVector = Icons.Default.PlayArrow,
                    contentDescription = null,
                    modifier = Modifier.size(34.dp),
                )
            }
        }
        Column(modifier = Modifier.weight(1f)) {
            Text(
                text = media.title,
                fontWeight = FontWeight.Bold,
                maxLines = 2,
                overflow = TextOverflow.Ellipsis,
            )
            if (media.artist.isNotBlank()) {
                Text(
                    text = media.artist,
                    maxLines = 1,
                    overflow = TextOverflow.Ellipsis,
                )
            }
            Text(
                text = media.playbackStatus.replaceFirstChar { it.uppercase() },
                style = MaterialTheme.typography.bodySmall,
            )
        }
    }
}

@Composable
private fun MediaTimeline(state: AppUiState) {
    val media = state.media
    val durationMs = media.durationMs ?: return
    if (durationMs <= 0) return

    var positionMs by remember(media.sessionId, media.positionMs, media.durationMs) {
        mutableFloatStateOf(media.positionMs?.toFloat() ?: 0f)
    }
    var dragging by remember { mutableStateOf(false) }

    LaunchedEffect(
        media.positionMs,
        media.durationMs,
        media.playbackStatus,
        media.timelineReceivedAtElapsedMs,
        dragging,
    ) {
        while (true) {
            if (!dragging) {
                positionMs = TimelinePredictor.positionMs(media, SystemClock.elapsedRealtime()).toFloat()
            }
            if (!media.isPlaying) break
            delay(250)
        }
    }

    Slider(
        value = positionMs,
        onValueChange = { value ->
            dragging = true
            positionMs = value
        },
        onValueChangeFinished = {
            dragging = false
            RemoteRepository.media("seek", positionMs = positionMs.toLong())
        },
        valueRange = 0f..durationMs.toFloat().coerceAtLeast(1f),
        enabled = state.connected && media.canSeek,
    )
    Row(modifier = Modifier.fillMaxWidth()) {
        Text(text = formatTime(positionMs.toLong()), style = MaterialTheme.typography.labelSmall)
        Spacer(modifier = Modifier.weight(1f))
        Text(text = formatTime(durationMs), style = MaterialTheme.typography.labelSmall)
    }
}

@Composable
private fun MediaControls(state: AppUiState) {
    val media = state.media
    Row(
        modifier = Modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.SpaceEvenly,
    ) {
        ControlTextButton(
            label = R.string.rewind_ten,
            enabled = state.connected && media.canSeek,
            onClick = { RemoteRepository.media("seekBy", offsetMs = -10_000) },
        )
        ControlIconButton(
            icon = Icons.Default.SkipPrevious,
            label = R.string.previous,
            enabled = state.connected && media.canPrevious,
            onClick = { RemoteRepository.media("previous") },
        )
        ControlIconButton(
            icon = if (media.isPlaying) Icons.Default.Pause else Icons.Default.PlayArrow,
            label = if (media.isPlaying) R.string.pause else R.string.play,
            enabled = state.connected && media.hasSession,
            onClick = { RemoteRepository.media(if (media.isPlaying) "pause" else "play") },
        )
        ControlIconButton(
            icon = Icons.Default.SkipNext,
            label = R.string.next,
            enabled = state.connected && media.canNext,
            onClick = { RemoteRepository.media("next") },
        )
        ControlTextButton(
            label = R.string.forward_ten,
            enabled = state.connected && media.canSeek,
            onClick = { RemoteRepository.media("seekBy", offsetMs = 10_000) },
        )
    }
}

@Composable
private fun ControlTextButton(label: Int, enabled: Boolean, onClick: () -> Unit) {
    OutlinedButton(
        onClick = onClick,
        enabled = enabled,
        contentPadding = PaddingValues(horizontal = 10.dp),
    ) { Text(stringResource(label)) }
}

@Composable
private fun ControlIconButton(
    icon: ImageVector,
    label: Int,
    enabled: Boolean,
    onClick: () -> Unit,
) {
    OutlinedButton(
        onClick = onClick,
        enabled = enabled,
        contentPadding = PaddingValues(horizontal = 10.dp),
    ) {
        Icon(
            imageVector = icon,
            contentDescription = stringResource(label),
            modifier = Modifier.size(24.dp),
        )
    }
}

@Composable
private fun VolumeCard(state: AppUiState) {
    var volume by remember(state.volume.level) { mutableFloatStateOf(state.volume.level) }
    Card {
        Column(
            modifier = Modifier.padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(8.dp),
        ) {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Text(
                    text = stringResource(R.string.windows_volume),
                    fontWeight = FontWeight.SemiBold,
                    modifier = Modifier.weight(1f),
                )
                Text(
                    text = NumberFormat.getPercentInstance(Locale.getDefault()).format(volume.toDouble()),
                )
            }
            Slider(
                value = volume,
                onValueChange = { volume = it },
                onValueChangeFinished = { RemoteRepository.volume("set", level = volume) },
                enabled = state.connected,
            )
            Button(
                onClick = { RemoteRepository.volume("toggleMute") },
                enabled = state.connected,
                modifier = Modifier.fillMaxWidth(),
            ) {
                Text(
                    stringResource(
                        if (state.volume.muted) R.string.unmute_windows else R.string.mute_windows,
                    ),
                )
            }
        }
    }
}

@Composable
private fun ConnectionSettingsDialog(
    reverseEnabled: Boolean,
    reverseHost: String,
    onReverseEnabledChanged: (Boolean) -> Unit,
    onReverseHostChanged: (String) -> Unit,
    onApply: () -> Unit,
    onDismiss: () -> Unit,
) {
    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text(stringResource(R.string.connection_settings)) },
        text = {
            Column(verticalArrangement = Arrangement.spacedBy(10.dp)) {
                Text(stringResource(R.string.connection_settings_explanation))
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Text(
                        text = stringResource(R.string.phone_connects_to_windows),
                        modifier = Modifier.weight(1f),
                    )
                    Switch(
                        checked = reverseEnabled,
                        onCheckedChange = onReverseEnabledChanged,
                    )
                }
                OutlinedTextField(
                    value = reverseHost,
                    onValueChange = onReverseHostChanged,
                    label = { Text(stringResource(R.string.windows_fallback_ip)) },
                    placeholder = { Text(stringResource(R.string.ip_example)) },
                    singleLine = true,
                )
            }
        },
        confirmButton = {
            TextButton(onClick = { onApply(); onDismiss() }) {
                Text(stringResource(R.string.save))
            }
        },
        dismissButton = {
            TextButton(onClick = onDismiss) { Text(stringResource(R.string.cancel)) }
        },
    )
}

@Composable
private fun ConnectionDiagnosticsDialog(onDismiss: () -> Unit) {
    val context = LocalContext.current
    val report = remember { ConnectionDiagnostics.report(context) }
    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text(stringResource(R.string.connection_diagnostics)) },
        text = {
            Column(
                modifier = Modifier.verticalScroll(rememberScrollState()),
                verticalArrangement = Arrangement.spacedBy(10.dp),
            ) {
                Text(stringResource(R.string.connection_diagnostics_explanation))
                Text(report, style = MaterialTheme.typography.bodySmall)
            }
        },
        confirmButton = {
            TextButton(onClick = {
                context.startActivity(
                    Intent.createChooser(
                        Intent(Intent.ACTION_SEND)
                            .setType("text/plain")
                            .putExtra(Intent.EXTRA_TEXT, report),
                        context.getString(R.string.share_diagnostics),
                    ).addFlags(Intent.FLAG_ACTIVITY_NEW_TASK),
                )
            }) { Text(stringResource(R.string.share_diagnostics)) }
        },
        dismissButton = {
            TextButton(onClick = onDismiss) { Text(stringResource(R.string.cancel)) }
        },
    )
}

@Composable
private fun PairingCard(
    state: AppUiState,
    onOpenConnectionSettings: () -> Unit,
) {
    var code by remember { mutableStateOf("") }
    Card(
        colors = CardDefaults.cardColors(
            containerColor = MaterialTheme.colorScheme.primaryContainer,
        ),
    ) {
        Column(
            modifier = Modifier.padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(8.dp),
        ) {
            Text(stringResource(R.string.secure_pairing), fontWeight = FontWeight.SemiBold)
            if (state.pairedComputer == null) {
                Text(stringResource(R.string.first_run_step_one))
                Text(stringResource(R.string.first_run_step_two))
                OutlinedTextField(
                    value = code,
                    onValueChange = { value -> code = value.filter(Char::isDigit).take(6) },
                    label = { Text(stringResource(R.string.pairing_code)) },
                    singleLine = true,
                    modifier = Modifier.fillMaxWidth(),
                )
                Button(
                    onClick = { RemoteRepository.beginPairing(code) },
                    enabled = code.length == 6,
                    modifier = Modifier.fillMaxWidth(),
                ) { Text(stringResource(R.string.search_and_pair)) }
                Text(stringResource(R.string.first_run_step_three))
                Text(stringResource(R.string.pairing_hint))
                OutlinedButton(
                    onClick = onOpenConnectionSettings,
                    modifier = Modifier.fillMaxWidth(),
                ) {
                    Text(stringResource(R.string.connection_settings))
                }
                OutlinedButton(
                    onClick = { RemoteRepository.resetPairing() },
                    modifier = Modifier.fillMaxWidth(),
                ) {
                        Text(stringResource(R.string.reset_pairing))
                }
            } else {
                Text(stringResource(R.string.paired_with, state.pairedComputer))
                OutlinedButton(onClick = { RemoteRepository.resetPairing() }) {
                    Text(stringResource(R.string.forget_computer))
                }
            }
        }
    }
}

private fun formatTime(milliseconds: Long): String {
    val seconds = (milliseconds / 1_000).coerceAtLeast(0)
    return String.format(Locale.US, "%d:%02d", seconds / 60, seconds % 60)
}
