// ═══════════════════════════════════════════════════════════════════
//  timeline.js  –  Video Timeline Marker (ASP.NET MVC edition)
//  Reads window.VIDEO_ID, window.INIT_SEGMENTS, window.VIDEO_DURATION
//  Communicates with /api/segments/* REST endpoints
// ═══════════════════════════════════════════════════════════════════

(function () {
    'use strict';

    // ── Constants ──────────────────────────────────────────────────────
    var NORMAL_COLORS = [
        '#38bdf8','#4ade80','#a78bfa','#facc15',
        '#34d399','#fb923c','#818cf8','#a3e635',
        '#2dd4bf','#fbbf24','#e879f9','#6ee7b7'
    ];
    var INCIDENT_COLOR = '#ef4444';

    // ── State ──────────────────────────────────────────────────────────
    var duration       = window.VIDEO_DURATION || 0;
    var segments       = [];          // mirrors DB
    var normalColorIdx = 0;
    var pendingStart   = null;
    var pendingEnd     = null;
    var segType        = 'normal';
    var editingId      = null;

    // ── Helpers ────────────────────────────────────────────────────────
    function G(id) { return document.getElementById(id); }

    function fmt(sec) {
        if (sec == null || !isFinite(sec)) return '--:--';
        var h  = Math.floor(sec / 3600);
        var m  = Math.floor((sec % 3600) / 60);
        var s  = Math.floor(sec % 60);
        var mm = ('0' + m).slice(-2);
        var ss = ('0' + s).slice(-2);
        return h > 0 ? (h + ':' + mm + ':' + ss) : (mm + ':' + ss);
    }

    function toPct(t) { return (t / duration) * 100; }

    function pxToTime(px) {
        var w = G('timeline-wrap').getBoundingClientRect().width;
        return Math.max(0, Math.min(duration, (px / w) * duration));
    }

    var toastTimer;
    function showToast(msg, ok) {
        var el = G('toast');
        el.textContent = msg;
        el.className   = 'show' + (ok ? ' ok' : '');
        clearTimeout(toastTimer);
        toastTimer = setTimeout(function () { el.className = ''; }, 2700);
    }

    function hasOverlap(start, end, excludeId) {
        return segments.some(function (s) {
            if (excludeId != null && s.id === excludeId) return false;
            return start < s.endTime && end > s.startTime;
        });
    }

    function setSaving(on) {
        G('saving-indicator').style.display = on ? 'block' : 'none';
    }

    // ── API helpers ───────────────────────────────────────────────────
    var VIDEO_ID = window.VIDEO_ID;

    function apiCall(method, url, body) {
        var opts = {
            method: method,
            headers: { 'Content-Type': 'application/json' }
        };
        if (body !== undefined) opts.body = JSON.stringify(body);
        return fetch(url, opts);
    }

    function apiAddSegment(data) {
        return apiCall('POST', '/api/segments', data);
    }
    function apiDeleteSegment(id) {
        return apiCall('DELETE', '/api/segments/' + id);
    }
    function apiUpdateLabel(id, label) {
        return apiCall('PUT', '/api/segments/' + id, { label: label });
    }
    function apiUpdateDuration(dur) {
        return apiCall('PATCH', '/api/segments/video/' + VIDEO_ID + '/duration', { duration: dur });
    }

    // ── DOM refs ──────────────────────────────────────────────────────
    var videoEl    = G('video');
    var tlWrap     = G('timeline-wrap');
    var playheadEl = G('playhead');
    var pendingInd = G('pending-ind');
    var ticksEl    = G('ticks');
    var ctDisp     = G('ct-disp');
    var durDisp    = G('dur-disp');
    var secStats   = G('sec-stats');
    var segList    = G('seg-list');
    var selS       = G('sel-s');
    var selE       = G('sel-e');
    var selD       = G('sel-d');
    var overlapW   = G('overlap-warn');
    var labelInput = G('label-input');
    var tooltip    = G('tooltip');
    var editModal  = G('edit-modal');
    var editInput  = G('edit-input');

    // ── Bootstrap from server-seeded data ────────────────────────────
    function init() {
        segments = (window.INIT_SEGMENTS || []).slice();

        // Assign colorIdx past already-used normal segments
        segments.forEach(function (s) {
            if (s.type !== 'incident') normalColorIdx++;
        });

        if (duration > 0) {
            durDisp.textContent = fmt(duration);
            buildTicks();
        }

        render();
        updateSel();
    }

    // ── Build tick marks ─────────────────────────────────────────────
    function buildTicks() {
        ticksEl.innerHTML = '';
        var step = duration <= 30  ? 5
                 : duration <= 90  ? 10
                 : duration <= 300 ? 30
                 : duration <= 1800 ? 60 : 300;
        for (var t = 0; t <= duration; t += step) {
            var el = document.createElement('span');
            el.className   = 'tick';
            el.style.left  = toPct(t) + '%';
            el.textContent = fmt(t);
            ticksEl.appendChild(el);
        }
    }

    // ── Render timeline segments ──────────────────────────────────────
    function render() {
        tlWrap.querySelectorAll('.seg-block,.pend-preview').forEach(function (el) { el.remove(); });

        segments.forEach(function (seg) {
            var isInc = seg.type === 'incident';
            var col   = seg.color;
            var div   = document.createElement('div');
            div.className = 'seg-block';
            div.style.cssText = [
                'position:absolute;top:0;bottom:0;',
                'left:' + toPct(seg.startTime) + '%;',
                'width:' + toPct(seg.endTime - seg.startTime) + '%;',
                'background:' + col + '42;',
                'border-left:3px solid ' + col + ';',
                'border-right:3px solid ' + col + ';',
                isInc
                    ? 'background-image:repeating-linear-gradient(135deg,transparent,transparent 5px,' + col + '28 5px,' + col + '28 10px);'
                    : '',
                'z-index:3;cursor:pointer;overflow:hidden;',
                'display:flex;align-items:center;padding:0 5px;'
            ].join('');

            if (seg.label) {
                var lbl = document.createElement('span');
                lbl.style.cssText = 'font-size:.58rem;font-weight:700;color:' + col + ';white-space:nowrap;overflow:hidden;text-overflow:ellipsis;pointer-events:none;';
                lbl.textContent = seg.label;
                div.appendChild(lbl);
            }

            div.title = (isInc ? '⚠️ ' : '🏷 ') + (seg.label || '(chưa đặt tên)') + ' | ' + fmt(seg.startTime) + ' → ' + fmt(seg.endTime);
            div.addEventListener('click', function (e) {
                e.stopPropagation();
                videoEl.currentTime = seg.startTime;
            });
            tlWrap.appendChild(div);
        });

        // Pending preview
        if (pendingStart !== null && pendingEnd !== null) {
            var ps  = Math.min(pendingStart, pendingEnd);
            var pe  = Math.max(pendingStart, pendingEnd);
            var col = segType === 'incident' ? INCIDENT_COLOR : '#22c55e';
            var pre = document.createElement('div');
            pre.className = 'pend-preview';
            pre.style.cssText = 'position:absolute;top:0;bottom:0;left:' + toPct(ps) + '%;width:' + toPct(pe - ps) + '%;background:' + col + '1a;border:2px dashed ' + col + ';z-index:4;pointer-events:none;border-radius:4px;';
            tlWrap.appendChild(pre);
        }

        // Pending start marker
        if (pendingStart !== null) {
            pendingInd.style.display = 'block';
            pendingInd.style.left    = toPct(pendingStart) + '%';
            pendingInd.setAttribute('data-time', fmt(pendingStart));
        } else {
            pendingInd.style.display = 'none';
        }

        updateStats();
    }

    // ── Update selection display ──────────────────────────────────────
    function updateSel() {
        selS.textContent = pendingStart !== null ? fmt(pendingStart) : '--:--';
        selE.textContent = pendingEnd   !== null ? fmt(pendingEnd)   : '--:--';

        if (pendingStart !== null && pendingEnd !== null) {
            selD.textContent = fmt(Math.abs(pendingEnd - pendingStart));
            var ps = Math.min(pendingStart, pendingEnd);
            var pe = Math.max(pendingStart, pendingEnd);
            overlapW.style.display = hasOverlap(ps, pe) ? 'block' : 'none';
        } else {
            selD.textContent = '--:--';
            overlapW.style.display = 'none';
        }

        G('btn-set-start').classList.toggle('lit', pendingStart !== null);
        G('btn-set-end').classList.toggle('lit', pendingEnd !== null && pendingStart !== null);
    }

    // ── Stats + segment list ──────────────────────────────────────────
    function updateStats() {
        var count   = segments.length;
        var inc     = segments.filter(function (s) { return s.type === 'incident'; }).length;
        var total   = segments.reduce(function (a, s) { return a + (s.endTime - s.startTime); }, 0);
        var pct     = duration > 0 ? ((total / duration) * 100).toFixed(1) : '0.0';

        G('st-count').textContent = count;
        G('st-inc').textContent   = inc;
        G('st-total').textContent = fmt(total);
        G('st-pct').textContent   = pct + '%';

        secStats.classList.toggle('visible', count > 0);

        segList.innerHTML = '';
        segments.forEach(function (seg, i) {
            var isInc = seg.type === 'incident';
            var card  = document.createElement('div');
            card.className = 'seg-card';
            card.style.borderLeftColor = seg.color;
            card.innerHTML =
                '<div class="seg-left">' +
                    '<div class="seg-num">#' + (i + 1) + '</div>' +
                    '<div class="seg-dot" style="background:' + seg.color + '"></div>' +
                '</div>' +
                '<div class="seg-info">' +
                    '<span class="seg-badge ' + (isInc ? 'badge-i' : 'badge-n') + '">' +
                        (isInc ? '⚠️ Sự cố' : '🏷 Bình thường') +
                    '</span>' +
                    '<div class="seg-name' + (seg.label ? '' : ' empty') + '">' +
                        (seg.label || 'Chưa đặt tên') +
                    '</div>' +
                    '<div class="seg-times">' + fmt(seg.startTime) + ' → ' + fmt(seg.endTime) + '</div>' +
                    '<div class="seg-dur">⏱ ' + fmt(seg.endTime - seg.startTime) + '</div>' +
                '</div>' +
                '<div class="seg-actions">' +
                    '<button class="seg-btn sb-edit" data-id="' + seg.id + '">✏️</button>' +
                    '<button class="seg-btn sb-del"  data-id="' + seg.id + '">🗑</button>' +
                '</div>';
            segList.appendChild(card);
        });

        segList.querySelectorAll('.sb-del').forEach(function (btn) {
            btn.addEventListener('click', function () {
                deleteSegment(+btn.getAttribute('data-id'));
            });
        });
        segList.querySelectorAll('.sb-edit').forEach(function (btn) {
            btn.addEventListener('click', function () {
                openEditModal(+btn.getAttribute('data-id'));
            });
        });
    }

    // ── Set Start ─────────────────────────────────────────────────────
    G('btn-set-start').addEventListener('click', function () {
        if (!duration) return;
        pendingStart = videoEl.currentTime;
        pendingEnd   = null;
        overlapW.style.display = 'none';
        updateSel();
        render();
        showToast('📍 Điểm đầu: ' + fmt(pendingStart), true);
    });

    // ── Set End (validate + save to API) ──────────────────────────────
    G('btn-set-end').addEventListener('click', function () {
        if (!duration) return;
        if (pendingStart === null) { showToast('⚠️ Hãy đặt điểm đầu trước!'); return; }

        var t     = videoEl.currentTime;
        var start = Math.min(pendingStart, t);
        var end   = Math.max(pendingStart, t);

        if (end - start < 0.1) { showToast('⚠️ Đoạn quá ngắn!'); return; }
        if (hasOverlap(start, end)) {
            showToast('❌ Đoạn đè lên đoạn đã có! Hãy chọn vùng khác.');
            pendingEnd = t; updateSel(); render();
            return;
        }

        var color = segType === 'incident'
            ? INCIDENT_COLOR
            : NORMAL_COLORS[normalColorIdx++ % NORMAL_COLORS.length];

        var payload = {
            videoSessionId: VIDEO_ID,
            label:     labelInput.value.trim(),
            startTime: start,
            endTime:   end,
            type:      segType,
            color:     color
        };

        setSaving(true);
        G('btn-set-end').disabled = true;

        apiAddSegment(payload).then(function (res) {
            if (res.status === 409) {
                return res.json().then(function (err) {
                    showToast('❌ ' + (err.message || 'Đoạn đè lên nhau!'));
                });
            }
            if (!res.ok) { showToast('❌ Lỗi lưu đoạn!'); return; }
            return res.json().then(function (saved) {
                segments.push(saved);
                segments.sort(function (a, b) { return a.startTime - b.startTime; });
                pendingStart = null; pendingEnd = null;
                labelInput.value = '';
                overlapW.style.display = 'none';
                updateSel(); render();
                showToast('✅ Đã lưu: ' + fmt(saved.startTime) + ' → ' + fmt(saved.endTime), true);
            });
        }).catch(function () {
            showToast('❌ Lỗi kết nối!');
        }).then(function () {
            setSaving(false);
            G('btn-set-end').disabled = false;
        });
    });

    // ── Delete a segment ──────────────────────────────────────────────
    function deleteSegment(id) {
        setSaving(true);
        apiDeleteSegment(id).then(function (res) {
            if (!res.ok) { showToast('❌ Lỗi xoá!'); return; }
            segments = segments.filter(function (s) { return s.id !== id; });
            render();
            showToast('↩ Đã xoá đoạn', false);
        }).catch(function () {
            showToast('❌ Lỗi kết nối!');
        }).then(function () { setSaving(false); });
    }

    // ── Edit label modal ──────────────────────────────────────────────
    function openEditModal(id) {
        var seg = segments.find(function (s) { return s.id === id; });
        if (!seg) return;
        editingId = id;
        editInput.value = seg.label || '';
        editModal.classList.add('show');
        editInput.focus();
    }

    G('modal-save').addEventListener('click', function () {
        var seg = segments.find(function (s) { return s.id === editingId; });
        if (!seg) { editModal.classList.remove('show'); return; }
        var newLabel = editInput.value.trim();
        setSaving(true);
        apiUpdateLabel(editingId, newLabel).then(function (res) {
            if (!res.ok) { showToast('❌ Lỗi cập nhật nhãn!'); return; }
            seg.label = newLabel;
            render();
            showToast('✅ Đã cập nhật nhãn', true);
        }).catch(function () {
            showToast('❌ Lỗi kết nối!');
        }).then(function () { setSaving(false); });
        editModal.classList.remove('show');
        editingId = null;
    });

    G('modal-cancel').addEventListener('click', function () {
        editModal.classList.remove('show'); editingId = null;
    });
    editModal.addEventListener('click', function (e) {
        if (e.target === editModal) { editModal.classList.remove('show'); editingId = null; }
    });
    editInput.addEventListener('keydown', function (e) {
        if (e.key === 'Enter')  G('modal-save').click();
        if (e.key === 'Escape') G('modal-cancel').click();
    });

    // ── Type selector ─────────────────────────────────────────────────
    window.setType = function (type) {
        segType = type;
        G('type-normal').className   = 'type-btn' + (type === 'normal'   ? ' on-normal'   : '');
        G('type-incident').className = 'type-btn' + (type === 'incident' ? ' on-incident' : '');
    };

    // ── Timeline click → seek ─────────────────────────────────────────
    tlWrap.addEventListener('click', function (e) {
        if (!duration) return;
        var rect = tlWrap.getBoundingClientRect();
        videoEl.currentTime = pxToTime(e.clientX - rect.left);
    });

    // Hover tooltip
    tlWrap.addEventListener('mousemove', function (e) {
        if (!duration) return;
        var rect = tlWrap.getBoundingClientRect();
        tooltip.style.display = 'block';
        tooltip.style.left    = (e.clientX + 14) + 'px';
        tooltip.style.top     = (e.clientY - 34) + 'px';
        tooltip.textContent   = fmt(pxToTime(e.clientX - rect.left));
    });
    tlWrap.addEventListener('mouseleave', function () { tooltip.style.display = 'none'; });

    // ── Playhead sync ─────────────────────────────────────────────────
    videoEl.addEventListener('timeupdate', function () {
        if (!duration) return;
        playheadEl.style.left = toPct(videoEl.currentTime) + '%';
        ctDisp.textContent    = fmt(videoEl.currentTime);
    });

    // ── Video metadata loaded → update duration ───────────────────────
    videoEl.addEventListener('loadedmetadata', function () {
        duration = videoEl.duration;
        durDisp.textContent = fmt(duration);
        buildTicks();
        render();
        // Patch duration to server if not already saved
        if (Math.abs((window.VIDEO_DURATION || 0) - duration) > 0.5) {
            apiUpdateDuration(duration);
        }
    });

    // ── Toolbar buttons ───────────────────────────────────────────────
    G('btn-play').addEventListener('click', function () {
        videoEl.paused ? videoEl.play() : videoEl.pause();
    });

    G('btn-undo').addEventListener('click', function () {
        if (!segments.length) { showToast('Không có đoạn nào!'); return; }
        var last = segments[segments.length - 1];
        deleteSegment(last.id);
    });

    G('btn-clear').addEventListener('click', function () {
        if (!segments.length) return;
        if (!confirm('Xoá tất cả đoạn đã đánh dấu?')) return;
        // Delete all one by one
        var ids = segments.map(function (s) { return s.id; });
        setSaving(true);
        var chain = Promise.resolve();
        ids.forEach(function (id) {
            chain = chain.then(function () { return apiDeleteSegment(id); });
        });
        chain.then(function () {
            segments = []; pendingStart = null; pendingEnd = null;
            overlapW.style.display = 'none';
            updateSel(); render();
            showToast('🗑 Đã xoá tất cả');
        }).catch(function () {
            showToast('❌ Lỗi khi xoá!');
        }).then(function () { setSaving(false); });
    });

    G('btn-export').addEventListener('click', function () {
        if (!segments.length) { showToast('Chưa có đoạn nào để xuất!'); return; }
        var total = segments.reduce(function (a, s) { return a + (s.endTime - s.startTime); }, 0);
        var data  = {
            exported_at:        new Date().toISOString(),
            video_session_id:   VIDEO_ID,
            video_duration_sec: +duration.toFixed(2),
            video_duration_fmt: fmt(duration),
            segment_count:      segments.length,
            incident_count:     segments.filter(function (s) { return s.type === 'incident'; }).length,
            total_marked_sec:   +total.toFixed(2),
            total_marked_fmt:   fmt(total),
            coverage_pct:       +((total / duration) * 100).toFixed(2),
            segments: segments.map(function (seg, i) {
                return {
                    index:        i + 1,
                    type:         seg.type,
                    label:        seg.label || '',
                    color:        seg.color,
                    start_sec:    +seg.startTime.toFixed(2),
                    end_sec:      +seg.endTime.toFixed(2),
                    duration_sec: +(seg.endTime - seg.startTime).toFixed(2),
                    start_fmt:    fmt(seg.startTime),
                    end_fmt:      fmt(seg.endTime),
                    duration_fmt: fmt(seg.endTime - seg.startTime)
                };
            })
        };
        var blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' });
        var a    = document.createElement('a');
        a.href     = URL.createObjectURL(blob);
        a.download = 'segments-video-' + VIDEO_ID + '.json';
        a.click();
        showToast('📋 Đã xuất JSON!', true);
    });

    // ── Keyboard shortcuts ────────────────────────────────────────────
    document.addEventListener('keydown', function (e) {
        if (!duration) return;
        if (['INPUT', 'TEXTAREA'].indexOf(document.activeElement.tagName) !== -1) return;
        switch (e.code) {
            case 'Space':
                e.preventDefault();
                videoEl.paused ? videoEl.play() : videoEl.pause();
                break;
            case 'KeyS': G('btn-set-start').click(); break;
            case 'KeyE': G('btn-set-end').click();   break;
            case 'ArrowLeft':
                e.preventDefault();
                videoEl.currentTime = Math.max(0, videoEl.currentTime - 5);
                break;
            case 'ArrowRight':
                e.preventDefault();
                videoEl.currentTime = Math.min(duration, videoEl.currentTime + 5);
                break;
        }
    });

    // ── Start ─────────────────────────────────────────────────────────
    init();

})();
