﻿(function () {
    var setMessages = window.setMessages = function (messages) {
        console.debug("Setting " + (messages ? messages.length : 0) + " messages");
        document.body.innerHTML = "";
        _addMessagessCore(messages, null);

        if (messages && messages.length > 0) {
            _showItem(messages[messages.length - 1].Id, false);
        }
    }

    function _getTopId() {
        var e = document.elementFromPoint(8, 8);
        while (e) {
            if (e.classList.contains("talk")) {
                return parseInt(e.getAttribute("data-message-id"), 10);
            }

            e = e.parentElement;
        }
        return -1;
    }

    function _showItem(id, top) {
        if (!id) {
            return;
        }

        console.debug("showing item[" + id + "] to " + (top ? "top" : "bottom"));

        var talk = document.getElementById('talk' + id);
        if (!talk) {
            console.warn("#talk" + id + " is not found");
            return;
        }

        var st;
        if (top) {
            st = talk.offsetTop;
        } else {
            st = talk.offsetTop + document.body.clientHeight - talk.clientHeight;
        }
        document.body.scrollTop = st;
    }

    function _addMessagessCore(messages, merged) {
        if (messages) {
            var b = document.body;
            var j = 0;
            for (var i = 0; i < messages.length; i++) {
                var m = messages[i];

                var id = m.Id;
                var avatarUrl = m.Avatar;
                var displayName = m.DisplayName;
                var publishedAt = m.PublishedAt;
                var content = m.Content;
                var isMerged = m.IsMerged;

                // console.debug("Processing message[" + id + "]");

                for (; ;) {
                    var prev = b.children[j];
                    var aft = b.children[j + 1];
                    var pid = prev ? parseInt(prev.getAttribute("data-message-id"), 10) : -1;
                    var aid = aft ? parseInt(aft.getAttribute("data-message-id"), 10) : Number.MAX_VALUE;

                    if (!prev || (id != pid && !aft)) {
                        // console.debug("Appending message[" + id + "]");
                        document.body.appendChild(createTaklElement(m));
                        j++;
                        break;
                    } else if (id < pid) {
                        // console.debug("Inserting message[" + id + "] before " + pid);
                        document.body.insertBefore(createTaklElement(m), prev);
                        j++;
                        break;
                    } else if (id == pid) {
                        // console.debug("Replacing message[" + id + "]");
                        document.body.insertBefore(createTaklElement(m), prev);
                        prev.remove();
                        break;
                    } else if (id < aid) {
                        // console.debug("Inserting message[" + id + "] before " + aid);
                        document.body.insertBefore(createTaklElement(m), aft);
                        j++;
                        break;
                    } else {
                        j++;
                    }
                }
            }
        }
        updateContinued(merged);
    }

    window.addMessages = function (messages, merged) {
        console.debug("Adding " + (messages ? messages.length : 0) + " messages");
        var tid = _getTopId();
        var top = tid > 0;
        if (!top && messages && messages.length > 0) {
            tid = messages[messages.length - 1].Id;
        }
        _addMessagessCore(messages, merged);
        _showItem(tid, top);
    }

    var removeMessages = window.removeMessages = function (ids, merged) {
        console.debug("Removing " + (ids ? ids.length : 0) + " messages");
        if (ids) {
            var j = 0;
            for (var i = 0; i < ids.length; i++) {
                var talk = document.getElementById('talk' + ids[i]);

                if (talk) {
                    talk.remove();
                }
            }
        }
        updateContinued(merged);
    }

    function updateContinued(merged) {
        if (merged) {
            for (var i = 0; i < merged.length; i++) {
                var m = merged[i];
                var id = m.Id;
                var isMerged = m.IsMerged;

                var talk = document.getElementById('talk' + id);
                if (talk) {
                    talk.classList.remove(!isMerged ? "continued" : "not-continued");
                    talk.classList.add(isMerged ? "continued" : "not-continued");
                }
            }
        }
    }

    function createTaklElement(m) {
        var id = m.Id;
        var avatarUrl = m.Avatar;
        var displayName = m.DisplayName;
        var publishedAt = m.PublishedAt;
        var content = m.Content;
        var isMerged = m.IsMerged;
        var embeds = m.EmbedContents;

        var talk = document.createElement("div");
        talk.id = "talk" + id;
        talk.classList.add("talk");
        talk.classList.add(isMerged ? "continued" : "not-continued");
        talk.setAttribute("data-message-id", id);

        try {
            var avatar = document.createElement("div");
            avatar.classList.add("avatar");
            talk.appendChild(avatar);

            var imgLink = document.createElement("a");
            imgLink.classList.add("img-rounded");
            avatar.appendChild(imgLink);

            var img = document.createElement("img");
            img.src = avatarUrl;
            imgLink.appendChild(img);

            var message = document.createElement("div");
            message.classList.add("message");
            talk.appendChild(message);

            var speaker = document.createElement("div");
            speaker.classList.add("speaker");
            message.appendChild(speaker);

            var name = document.createElement("a");
            name.innerText = displayName;
            speaker.appendChild(name);

            var small = document.createElement("small");
            small.classList.add("timeleft", "text-muted");
            small.innerText = publishedAt;
            speaker.appendChild(small);

            var filteredText = document.createElement("div");
            filteredText.classList.add("filtered_text");
            filteredText.innerHTML = content;
            message.appendChild(filteredText);

            if (embeds && embeds.length > 0) {
                var ecs = document.createElement("div");
                ecs.classList.add("embed_contents");
                message.appendChild(ecs);

                try {
                    for (var i = 0; i < embeds.length; i++) {
                        var e = embeds[i];
                        if (!e) {
                            continue;
                        }
                        var d = e.data;
                        if (!d) {
                            continue;
                        }
                        var ec = document.createElement("div");
                        ec.classList.add("embed_content");
                        ecs.appendChild(ec);

                        switch (d.type) {
                            case 'MixedContent':
                                ec.appendChild(_createEmbedContent(d, false));
                                break;
                            case 'SingleImage':
                            case 'SingleVideo':
                            case 'SingleAudio':
                                ec.appendChild(_createEmbedContent(d, true));
                                break;
                            default:
                                console.warn("Unknown embed data: ", d);
                                break;
                        }
                    }
                } catch (ex) {
                    ecs.innerHTML = "";

                    var err = document.createElement('p');
                    err.innerText = ex;
                    ecs.appendChild(err);

                    var json = document.createElement('pre');
                    json.innerText = JSON.stringify(d);
                    ecs.appendChild(json);
                }
            }
        } catch (ex) {
            talk.innerText = ex;
        }

        return talk;
    }

    function _createEmbedContent(d, hideInfo) {
        var r = document.createElement("div");
        r.classList.add("embed-" + d.type.toLowerCase());

        if (!hideInfo) {
            var meta = document.createElement("div");
            meta.classList.add("meta");
            r.appendChild(meta);

            if (d.metadata_image) {
                var m = d.metadata_image;

                var thumb = document.createElement("div");
                thumb.classList.add("thumb");
                meta.appendChild(thumb);

                thumb.appendChild(_createMediaDiv(m, "embed-thumbnail"));
            }
            var info = document.createElement("div");
            info.classList.add("info");
            meta.appendChild(info);

            if (d.title) {
                var titleDiv = document.createElement("div");
                titleDiv.classList.add("title");
                info.appendChild(titleDiv);

                var titleLink = document.createElement("a");
                titleLink.href = d.url;
                titleDiv.appendChild(titleLink);

                var title = document.createElement("strong");
                title.innerText = d.title;
                titleLink.appendChild(title);
            }

            if (d.description) {
                var descriptionDiv = document.createElement("div");
                descriptionDiv.classList.add("description");
                info.appendChild(descriptionDiv);

                var description = document.createElement("p");
                description.innerText = d.description;
                descriptionDiv.appendChild(description);
            }
        }

        if (d.medias && d.medias.length > 0) {
            var medias = document.createElement("div");
            medias.classList.add("medias");
            r.appendChild(medias);

            for (var i = 0; i < d.medias.length; i++) {
                var m = d.medias[i];
                if (m) {
                    medias.appendChild(_createMediaDiv(m));
                }
            }
        }

        return r;
    }

    function _createMediaDiv(m, className) {
        var em = document.createElement("div");
        em.classList.add(className || "embed_media");

        var a = document.createElement("a");
        a.href = m.location || m.raw_url;
        em.appendChild(a);

        var img = document.createElement("img");
        img.classList.add("img-rounded");
        img.src = (m.thumbnail ? m.thumbnail.url : null) || m.raw_url;
        a.appendChild(img);

        return em;
    }

    document.addEventListener("DOMContentLoaded", function () {
        document.addEventListener("scroll", function () {
            var b = document.body;

            if (b.scrollHeight < b.clientHeight) {
                return;
            }

            if (b.scrollTop < 4) {
                console.log("Loading older messages.");
                location.href = "http://kokoro.io/client/control?event=prepend";
            }
            else if (b.scrollTop + b.clientHeight + 4 > b.scrollHeight) {
                console.log("Loading newer messages.");
                location.href = "http://kokoro.io/client/control?event=append";
            }
        });
    });
})();