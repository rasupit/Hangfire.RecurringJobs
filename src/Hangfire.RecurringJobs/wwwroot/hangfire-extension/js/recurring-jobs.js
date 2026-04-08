(function () {
  function debounce(callback, delay) {
    let timerId;

    return function () {
      const args = arguments;
      clearTimeout(timerId);
      timerId = window.setTimeout(function () {
        callback.apply(null, args);
      }, delay);
    };
  }

  function escapeJobId(jobId) {
    return window.CSS && window.CSS.escape ? window.CSS.escape(jobId) : jobId.replace(/"/g, '\\"');
  }

  function getApiBase() {
    const root = document.querySelector("[data-hfext-api-base]");
    return root ? root.getAttribute("data-hfext-api-base") || "" : "";
  }

  function setStatusMessage(message, succeeded, options) {
    const settings = options || {};
    const status = document.querySelector("[data-hfext-status-message]");
    if (status) {
      status.hidden = false;
      status.textContent = message;
      status.classList.remove("alert-success", "alert-danger");
      status.classList.add(succeeded ? "alert-success" : "alert-danger");
    }

    if (settings.toast !== false) {
      showToast(message, succeeded);
    }
  }

  function showToast(message, succeeded) {
    const region = document.querySelector("[data-hfext-toast-region]");
    if (!region) {
      return;
    }

    region.replaceChildren();

    const toast = document.createElement("div");
    toast.className = "hfext-toast " + (succeeded ? "is-success" : "is-error");

    const meta = document.createElement("div");
    meta.className = "hfext-toast-meta";

    const badge = document.createElement("span");
    badge.className = "badge " + (succeeded ? "text-bg-success" : "text-bg-danger");
    badge.textContent = succeeded ? "Success" : "Error";

    const time = document.createElement("span");
    time.className = "hfext-toast-time";
    time.textContent = new Date().toLocaleTimeString([], {
      hour: "2-digit",
      minute: "2-digit",
      second: "2-digit"
    });

    const body = document.createElement("p");
    body.className = "hfext-toast-message";
    body.textContent = message;

    meta.appendChild(badge);
    meta.appendChild(time);
    toast.appendChild(meta);
    toast.appendChild(body);
    region.appendChild(toast);

    window.requestAnimationFrame(function () {
      toast.classList.add("is-visible");
    });

    window.setTimeout(function () {
      toast.classList.remove("is-visible");
      window.setTimeout(function () {
        if (toast.parentNode === region) {
          region.removeChild(toast);
        }
      }, 180);
    }, 3600);
  }

  function setElementHidden(element, hidden) {
    if (!element) {
      return;
    }

    element.hidden = hidden;
  }

  function updateTriggerVisibility(row, job) {
    const triggerForm = row.querySelector("[data-hfext-trigger-form]");
    if (!triggerForm) {
      return;
    }

    setElementHidden(triggerForm, !!job.isDisabled || !!job.isSystemError);
  }

  function updateStatus(row, job) {
    row.setAttribute("data-is-disabled", String(!!job.isDisabled));
    row.setAttribute("data-is-system-error", String(!!job.isSystemError));

    const badge = row.querySelector("[data-hfext-status-badge]");
    if (badge) {
      badge.classList.remove("text-bg-success", "text-bg-secondary", "text-bg-danger");
      if (job.isSystemError) {
        badge.textContent = "Unavailable";
        badge.classList.add("text-bg-danger");
      } else if (job.isDisabled) {
        badge.textContent = "Disabled";
        badge.classList.add("text-bg-secondary");
      } else {
        badge.textContent = "Active";
        badge.classList.add("text-bg-success");
      }
    }

    const enabledInput = row.querySelector("[data-hfext-enabled-input]");
    if (enabledInput) {
      enabledInput.value = String(!!job.isDisabled);
    }

    const switchButton = row.querySelector("[data-hfext-inline-form='toggle'] .hfext-switch");
    if (switchButton) {
      switchButton.classList.toggle("is-on", !job.isDisabled);
      switchButton.classList.toggle("is-off", !!job.isDisabled);
      switchButton.setAttribute("aria-checked", String(!job.isDisabled));
      switchButton.setAttribute("aria-label", job.isDisabled ? "Enable " + job.id : "Disable " + job.id);
    }

    const switchLabel = row.querySelector(".hfext-switch-label");
    if (switchLabel) {
      switchLabel.textContent = job.isDisabled ? "Enable" : "Disable";
    }

    updateTriggerVisibility(row, job);
  }

  function updateRow(row, job) {
    row.setAttribute("data-job-id", job.id);

    const cronDisplay = row.querySelector("[data-hfext-cron-expression-display]");
    if (cronDisplay) {
      cronDisplay.textContent = job.cronExpression || "-";
    }

    const nextExecution = row.querySelector("[data-hfext-next-execution]");
    if (nextExecution) {
      nextExecution.textContent = "Next: " + (job.nextExecution ? new Date(job.nextExecution).toLocaleString() : "-");
    }

    const lastExecution = row.querySelector("[data-hfext-last-execution]");
    if (lastExecution) {
      lastExecution.textContent = "Last: " + (job.lastExecution ? new Date(job.lastExecution).toLocaleString() : "-");
    }

    const lastJobId = row.querySelector("[data-hfext-last-job-id]");
    if (lastJobId) {
      lastJobId.textContent = "Last job id: " + (job.lastJobId || "-");
    }

    const queue = row.querySelector("[data-hfext-queue]");
    if (queue) {
      queue.textContent = job.queue || "-";
    }

    const errorBadgeWrapper = row.querySelector("[data-hfext-error-badge-wrapper]");
    const errorText = row.querySelector("[data-hfext-error-text]");
    if (errorText) {
      errorText.textContent = job.error || "";
      setElementHidden(errorText, !job.error);
    }

    if (errorBadgeWrapper) {
      setElementHidden(errorBadgeWrapper, !job.error);
    }

    const jobType = row.querySelector("[data-hfext-job-type]");
    if (jobType) {
      jobType.textContent = job.jobType || "";
    }

    const methodName = row.querySelector("[data-hfext-method-name]");
    if (methodName) {
      methodName.textContent = job.methodName || "";
    }

    const editLink = row.querySelector("[data-hfext-edit-cron-link]");
    if (editLink) {
      editLink.setAttribute("data-hfext-job-type", job.jobType || "");
      editLink.setAttribute("data-hfext-method-name", job.methodName || "");
      editLink.setAttribute("data-hfext-cron-expression", job.cronExpression || "");
      editLink.setAttribute("data-hfext-is-disabled", String(!!job.isDisabled));
    }

    updateStatus(row, job);
  }

  function fetchJson(url, options) {
    return fetch(url, options).then(function (response) {
      return response.text().then(function (text) {
        const payload = text ? JSON.parse(text) : null;
        if (!response.ok) {
          const message = payload && payload.message ? payload.message : "The recurring job request could not be completed.";
          throw new Error(message);
        }

        return payload;
      });
    });
  }

  function refreshRow(jobId) {
    const apiBase = getApiBase();
    if (!apiBase) {
      return Promise.resolve();
    }

    return fetchJson(apiBase + "/" + encodeURIComponent(jobId), {
      headers: {
        Accept: "application/json"
      }
    }).then(function (job) {
      const row = document.querySelector('[data-hfext-job-row][data-job-id="' + escapeJobId(jobId) + '"]');
      if (row) {
        updateRow(row, job);
      }
    });
  }

  function renderPreview(editor, preview) {
    const summary = editor.querySelector("[data-hfext-cron-summary]");
    const description = editor.querySelector("[data-hfext-cron-description]");
    const occurrencesWrapper = editor.querySelector("[data-hfext-cron-occurrences-wrapper]");
    const occurrences = editor.querySelector("[data-hfext-cron-occurrences]");

    if (!summary || !description || !occurrencesWrapper || !occurrences) {
      return;
    }

    summary.textContent = preview.summary || "";
    summary.classList.toggle("is-valid", !!preview.isValid);
    summary.classList.toggle("is-invalid", !preview.isValid);

    if (preview.description) {
      description.hidden = false;
      description.textContent = preview.description;
    } else {
      description.hidden = true;
      description.textContent = "";
    }

    occurrences.replaceChildren();
    if (preview.upcomingOccurrences && preview.upcomingOccurrences.length > 0) {
      preview.upcomingOccurrences.forEach(function (occurrence) {
        const item = document.createElement("li");
        item.textContent = occurrence;
        occurrences.appendChild(item);
      });

      occurrencesWrapper.hidden = false;
    } else {
      occurrencesWrapper.hidden = true;
    }
  }

  function attachPreview(editor) {
    const input = editor.querySelector("[data-hfext-cron-input]");
    const previewUrl = editor.getAttribute("data-hfext-cron-preview-url");

    if (!input || !previewUrl) {
      return;
    }

    let activeRequest;

    const updatePreview = debounce(function () {
      if (activeRequest) {
        activeRequest.abort();
      }

      activeRequest = new AbortController();
      const url = new URL(previewUrl, window.location.origin);
      url.searchParams.set("cronExpression", input.value);

      fetch(url, {
        headers: {
          Accept: "application/json"
        },
        signal: activeRequest.signal
      })
        .then(function (response) {
          if (!response.ok) {
            throw new Error("Preview request failed.");
          }

          return response.json();
        })
        .then(function (preview) {
          renderPreview(editor, preview);
        })
        .catch(function (error) {
          if (error.name === "AbortError") {
            return;
          }

          renderPreview(editor, {
            isValid: false,
            summary: "Cron preview is temporarily unavailable.",
            description: null,
            upcomingOccurrences: []
          });
        });
    }, 250);

    input.addEventListener("input", updatePreview);
    updatePreview();
  }

  function attachInlineForms() {
    const apiBase = getApiBase();
    if (!apiBase) {
      return;
    }

    document.querySelectorAll("[data-hfext-inline-form]").forEach(function (form) {
      form.addEventListener("submit", function (event) {
        event.preventDefault();

        const formData = new FormData(form);
        const jobId = formData.get("id");
        if (!jobId) {
          return;
        }

        const operation = form.getAttribute("data-hfext-inline-form");
        let request;

        if (operation === "toggle") {
          const shouldEnable = formData.get("enabled") === "true";
          request = fetchJson(apiBase + "/" + encodeURIComponent(jobId) + "/" + (shouldEnable ? "enable" : "disable"), {
            method: "POST"
          });
        } else if (operation === "trigger") {
          request = fetchJson(apiBase + "/" + encodeURIComponent(jobId) + "/trigger", {
            method: "POST"
          });
        } else {
          return;
        }

        request
          .then(function (result) {
            const showToast = operation !== "toggle";
            setStatusMessage(result.message, !!result.succeeded, { toast: showToast });
            return refreshRow(String(jobId));
          })
          .catch(function (error) {
            setStatusMessage(error.message, false);
          });
      });
    });
  }

  function attachDialog() {
    const dialog = document.querySelector("[data-hfext-cron-dialog]");
    const apiBase = getApiBase();
    if (!dialog || typeof dialog.showModal !== "function" || !apiBase) {
      return;
    }

    const editor = dialog.querySelector("[data-hfext-cron-editor]");
    const idField = dialog.querySelector("[data-hfext-cron-id]");
    const input = dialog.querySelector("[data-hfext-cron-input]");
    const jobId = dialog.querySelector("[data-hfext-dialog-job-id]");
    const jobType = dialog.querySelector("[data-hfext-dialog-job-type]");
    const methodName = dialog.querySelector("[data-hfext-dialog-method-name]");
    const disabledNote = dialog.querySelector("[data-hfext-disabled-note]");
    const openFullPage = dialog.querySelector("[data-hfext-open-full-page]");

    if (!editor || !idField || !input || !jobId || !jobType || !methodName || !disabledNote || !openFullPage) {
      return;
    }

    document.querySelectorAll("[data-hfext-edit-cron-link]").forEach(function (link) {
      link.addEventListener("click", function (event) {
        event.preventDefault();

        idField.value = link.getAttribute("data-hfext-job-id") || "";
        input.value = link.getAttribute("data-hfext-cron-expression") || "";
        jobId.textContent = idField.value;
        jobType.textContent = link.getAttribute("data-hfext-job-type") || "";
        methodName.textContent = link.getAttribute("data-hfext-method-name") || "";
        disabledNote.hidden = link.getAttribute("data-hfext-is-disabled") !== "true";
        openFullPage.setAttribute("href", link.getAttribute("href") || "#");

        dialog.showModal();
        input.focus();
        input.select();
        input.dispatchEvent(new Event("input", { bubbles: true }));
      });
    });

    dialog.querySelectorAll("[data-hfext-close-dialog]").forEach(function (button) {
      button.addEventListener("click", function () {
        dialog.close();
      });
    });

    editor.addEventListener("submit", function (event) {
      if (editor.getAttribute("data-hfext-inline-save") !== "true") {
        return;
      }

      event.preventDefault();
      fetchJson(apiBase + "/" + encodeURIComponent(idField.value), {
        method: "PUT",
        headers: {
          "Content-Type": "application/json",
          Accept: "application/json"
        },
        body: JSON.stringify({
          cronExpression: input.value
        })
      })
        .then(function (result) {
          setStatusMessage(result.message, !!result.succeeded);
          return refreshRow(idField.value);
        })
        .then(function () {
          dialog.close();
        })
        .catch(function (error) {
          setStatusMessage(error.message, false);
          input.focus();
        });
    });
  }

  document.querySelectorAll("[data-hfext-cron-editor]").forEach(attachPreview);
  attachInlineForms();
  attachDialog();
})();
