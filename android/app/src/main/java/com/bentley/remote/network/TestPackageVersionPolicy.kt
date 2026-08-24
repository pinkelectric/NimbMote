package com.bentley.remote.network

/** Shows Bentley's own delivery card only for a strictly newer installed version. */
internal object TestPackageVersionPolicy {
    private val bentleyLabel = Regex(
        pattern = "^Bentley Remote v(\\d+)\\.(\\d+)\\.(\\d+)$",
        option = RegexOption.IGNORE_CASE,
    )

    fun shouldShow(label: String, installedVersion: String): Boolean {
        val candidate = bentleyLabel.matchEntire(label) ?: return true
        val installed = installedVersion.split('.').mapNotNull(String::toIntOrNull)
        if (installed.size != 3) return true
        val offered = candidate.groupValues.drop(1).map(String::toInt)
        return offered.zip(installed)
            .firstOrNull { (next, current) -> next != current }
            ?.let { (next, current) -> next > current }
            ?: false
    }
}
