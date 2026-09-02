package com.bentley.remote.network

/** Shows Deskora's own delivery card only for a strictly newer installed version. */
internal object TestPackageVersionPolicy {
    private val deskoraLabel = Regex(
        // A release may include a short human-readable note after its version.
        // It is still Deskora's own package and must not be shown after install.
        pattern = "^(?:NimbMote|Deskora|Bentley Remote) v(\\d+)\\.(\\d+)\\.(\\d+)(?:\\s+.*)?$",
        option = RegexOption.IGNORE_CASE,
    )

    fun shouldShow(label: String, installedVersion: String): Boolean {
        val candidate = deskoraLabel.matchEntire(label) ?: return true
        val installed = installedVersion.split('.').mapNotNull(String::toIntOrNull)
        if (installed.size != 3) return true
        val offered = candidate.groupValues.drop(1).map(String::toInt)
        return offered.zip(installed)
            .firstOrNull { (next, current) -> next != current }
            ?.let { (next, current) -> next > current }
            ?: false
    }
}
