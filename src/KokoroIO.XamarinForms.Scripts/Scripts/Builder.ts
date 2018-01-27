/// <reference path="Constants.ts" />

module Messages {
    function _padLeft(i: number, l: number): string {
        var offset = l == 1 ? 10 : l == 2 ? 100 : Math.pow(10, l);

        if (i > offset) {
            var s = i.toFixed(0);
            return s.substr(s.length - l, l);
        }

        return (i + offset).toFixed(0).substr(1, l);
    }

    function _createFAAnchor(url: string, faClass: string, disabled?: boolean): HTMLAnchorElement {
        let a = document.createElement("a");
        if (disabled) {
            a.href = "javascript:void(0)";
            a.classList.add("disabled");
        } else {
            a.href = url;
        }
        a.appendChild(_createFA(faClass));
        return a;
    }

    function _createFA(className: string): HTMLUnknownElement {
        const r = document.createElement("i");
        r.classList.add("fa");
        r.classList.add(className);
        return r;
    }

    export function createTaklElement(m: MessageInfo): HTMLDivElement {
        var id = m.Id;

        let talk = document.createElement("div");
        talk.classList.add("talk");
        talk.classList.add(m.IsMerged ? "continued" : "not-continued");
        if (id) {
            talk.id = "talk" + id;
            talk.setAttribute("data-message-id", id.toString());

            if (!m.IsDeleted) {
                if (IS_DESKTOP || IS_TABLET) {
                    let control = document.createElement("div");
                    control.classList.add("message-menu");

                    // reply
                    control.appendChild(_createFAAnchor(`http://kokoro.io/client/control?event=replyToMessage&id=${m.Id}`, "fa-reply"));

                    // copy
                    control.appendChild(_createFAAnchor(`http://kokoro.io/client/control?event=copyMessage&id=${m.Id}`, "fa-clipboard"));

                    // delete
                    control.appendChild(_createFAAnchor(`http://kokoro.io/client/control?event=deleteMessage&id=${m.Id}`, "fa-trash", !m.CanDelete));

                    talk.appendChild(control);
                } else {
                    let control = document.createElement("div");
                    control.classList.add("message-menu");
                    control.appendChild(_createFAAnchor(`http://kokoro.io/client/control?event=messageMenu&id=${m.Id}`, "fa-chevron-down"));
                    talk.appendChild(control);
                }
            }
        } else {
            var idempotentKey = m.IdempotentKey;
            if (idempotentKey) {
                talk.setAttribute("data-idempotent-key", idempotentKey);
            }
        }

        try {
            var avatar = document.createElement("div");
            avatar.classList.add("avatar");
            talk.appendChild(avatar);

            var profUrl = `https://kokoro.io/profiles/${m.ProfileId}`;

            var imgLink = document.createElement("a");
            imgLink.href = profUrl;
            imgLink.classList.add("img-rounded");
            avatar.appendChild(imgLink);

            var img = document.createElement("img");
            img.src = m.Avatar;
            imgLink.appendChild(img);

            var message = document.createElement("div");
            message.classList.add("message");
            talk.appendChild(message);

            var speaker = document.createElement("div");
            speaker.classList.add("speaker");
            message.appendChild(speaker);

            var name = document.createElement("a");
            name.innerText = m.DisplayName;
            name.href = profUrl;
            speaker.appendChild(name);

            if (m.IsBot) {
                var small = document.createElement("small");
                small.className = "label label-default";
                small.innerText = "bot";
                speaker.appendChild(small);
            }

            var small = document.createElement("small");
            small.classList.add("timeleft", "text-muted");
            if (m.PublishedAt) {
                try {
                    let d = new Date(Date.parse(m.PublishedAt));

                    small.innerText = (d.getFullYear() == new Date().getFullYear() ? '' : _padLeft(d.getFullYear(), 4) + '/')
                        + _padLeft(d.getMonth() + 1, 2)
                        + '/' + _padLeft(d.getDate(), 2)
                        + ' ' + _padLeft(d.getHours(), 2)
                        + ':' + _padLeft(d.getMinutes(), 2);

                    small.title = _padLeft(d.getFullYear(), 4)
                        + '/' + _padLeft(d.getMonth() + 1, 2)
                        + '/' + _padLeft(d.getDate(), 2)
                        + ' ' + _padLeft(d.getHours(), 2)
                        + ':' + _padLeft(d.getMinutes(), 2)
                        + ':' + _padLeft(d.getSeconds(), 2);
                } catch (ex) {
                    small.innerText = m.PublishedAt;
                }
            } else {
                small.innerText = "Now sending...";
            }
            speaker.appendChild(small);

            var filteredText = document.createElement("div");
            filteredText.classList.add(m.IsDeleted ? "deleted_text" : "filtered_text");
            filteredText.innerHTML = m.HtmlContent;
            message.appendChild(filteredText);

            if (m.EmbedContents && m.EmbedContents.length > 0) {
                var ecs = document.createElement("div");
                ecs.classList.add("embed_contents");
                message.appendChild(ecs);

                let d;
                try {
                    for (var i = 0; i < m.EmbedContents.length; i++) {
                        var e = m.EmbedContents[i];
                        if (!e) {
                            continue;
                        }
                        d = e.data;
                        if (!d) {
                            continue;
                        }
                        var ec = document.createElement("div");
                        ec.classList.add("embed_content");
                        ecs.appendChild(ec);

                        switch (d.type) {
                            case 'MixedContent':
                                ec.appendChild(_createEmbedContent(m, d, false));
                                break;
                            case 'SingleImage':
                            case 'SingleVideo':
                            case 'SingleAudio':
                                ec.appendChild(_createEmbedContent(m, d, true));
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

    function _createEmbedContent(message: MessageInfo, d: EmbedData, hideInfo: boolean): HTMLDivElement {
        var r = document.createElement("div");
        r.classList.add("embed-" + d.type.toLowerCase());

        if (!hideInfo) {
            var meta = document.createElement("div");
            meta.classList.add("meta");
            r.appendChild(meta);

            if (d.metadata_image) {
                var m = d.metadata_image;

                var thd = _createMediaDiv(m, d, message, "embed-thumbnail");

                if (thd) {
                    var thumb = document.createElement("div");
                    thumb.classList.add("thumb");
                    meta.appendChild(thumb);

                    thumb.appendChild(thd);
                }
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

                let description = document.createElement("p");

                let re = /(https?:\/\/[a-z0-9]+(?:[-.][a-z0-9]+)*(?:\/|[!$&-;=?-Z\\^_a-~]|%[A-F0-9]{2})*)/;

                let ary = d.description.split(re);

                if (ary && ary.length > 1) {
                    for (let i = 0; i < ary.length; i++) {
                        let t = ary[i];
                        if (i % 2 == 0) {
                            description.appendChild(document.createTextNode(t));
                        } else {
                            let a = document.createElement("a");
                            a.setAttribute("href", t);
                            a.appendChild(document.createTextNode(t));

                            description.appendChild(a);
                        }
                    }
                } else {
                    description.innerText = d.description;
                }

                descriptionDiv.appendChild(description);
            }
        }

        if (d.medias && d.medias.length > 0) {
            var medias = document.createElement("div");
            medias.classList.add("medias");

            for (var i = 0; i < d.medias.length; i++) {
                var m = d.medias[i];
                if (m) {
                    var md = _createMediaDiv(m, d, message);
                    if (md) {
                        medias.appendChild(md);
                    }
                }
            }

            if (medias.children.length > 0) {
                r.appendChild(medias);
            }
        }

        return r;
    }

    function _createMediaDiv(media: EmbedDataMedia, data: EmbedData, message: MessageInfo, className?: string): HTMLDivElement {
        var tu = (media.thumbnail ? media.thumbnail.url : null) || media.raw_url;

        if (!tu) {
            return null;
        }

        var em = document.createElement("div");
        em.classList.add(className || "embed_media");

        var a = document.createElement("a");
        a.href = media.location || media.raw_url || data.url;
        em.appendChild(a);

        var img = document.createElement("img");
        img.classList.add("img-rounded");
        img.src = tu;
        a.appendChild(img);

        var policies = [(message.IsNsfw ? "Restricted" : "Unknown"), media.restriction_policy, data.restriction_policy];

        if (policies.filter(p => p !== "Unknown")[0] === "Restricted") {
            em.classList.add("nsfw");

            var i = document.createElement("i");
            i.className = "nsfw-mark fa fa-exclamation-triangle";
            em.appendChild(i);
        }

        return em;
    }
}