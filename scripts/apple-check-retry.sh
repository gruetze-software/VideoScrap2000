#!/usr/bin/env bash
# Source this helper. Retry temporary Apple service failures, never bad signatures.
retry_apple_check() {
  local attempt result output delay
  output=$(mktemp)
  for attempt in 1 2 3 4 5 6; do
    if "$@" >"$output" 2>&1; then
      cat "$output"
      rm -f -- "$output"
      return 0
    else
      result=$?
    fi
    cat "$output"
    if [[ $attempt == 6 ]] || ! grep -Eiq 'NSURLErrorDomain|network connection|timed out|could not connect|couldn.t connect|HTTP[^0-9]*(429|502|503|504)|record not found' "$output"; then
      rm -f -- "$output"
      return "$result"
    fi
    case $attempt in 1) delay=5 ;; 2) delay=10 ;; 3) delay=20 ;; 4) delay=40 ;; *) delay=60 ;; esac
    echo "Temporary Apple service failure; retry $((attempt + 1))/6 in ${delay}s."
    sleep "$delay"
  done
}
