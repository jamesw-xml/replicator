{{- define "kurrentdb.name" -}}
kurrentdb
{{- end -}}

{{- define "kurrentdb.fullname" -}}
{{ printf "%s-%s" .Release.Name (include "kurrentdb.name" .) }}
{{- end -}}
