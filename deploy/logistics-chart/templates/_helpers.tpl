{{/* Chart name (optionally overridden). */}}
{{- define "logistics.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{/* Fully qualified app name. */}}
{{- define "logistics.fullname" -}}
{{- if .Values.fullnameOverride -}}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" -}}
{{- else -}}
{{- printf "%s-%s" .Release.Name (include "logistics.name" .) | trunc 63 | trimSuffix "-" -}}
{{- end -}}
{{- end -}}

{{/* Common labels. */}}
{{- define "logistics.labels" -}}
helm.sh/chart: {{ printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
app.kubernetes.io/part-of: logistics
{{- end -}}

{{/* Per-component selector labels. Usage: include "logistics.selectorLabels" (dict "ctx" . "component" "api") */}}
{{- define "logistics.selectorLabels" -}}
app.kubernetes.io/name: {{ include "logistics.name" .ctx }}
app.kubernetes.io/instance: {{ .ctx.Release.Name }}
app.kubernetes.io/component: {{ .component }}
{{- end -}}

{{/* Resolved service hostnames for the dependency subcharts (overridable via .Values.config). */}}
{{- define "logistics.neo4jHost" -}}
{{- default (printf "%s-neo4j" .Release.Name) .Values.config.neo4j.host -}}
{{- end -}}

{{- define "logistics.redisHost" -}}
{{- default (printf "%s-redis-master" .Release.Name) .Values.config.redis.host -}}
{{- end -}}

{{- define "logistics.rabbitmqHost" -}}
{{- default (printf "%s-rabbitmq" .Release.Name) .Values.config.rabbitmq.host -}}
{{- end -}}

{{- define "logistics.kafkaBootstrap" -}}
{{- default (printf "%s-kafka:9092" .Release.Name) .Values.config.kafka.bootstrap -}}
{{- end -}}

{{- define "logistics.schemaRegistryUrl" -}}
{{- default (printf "http://%s-schema-registry:8081" .Release.Name) .Values.config.kafka.schemaRegistryUrl -}}
{{- end -}}
