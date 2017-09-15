(function () {
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
            var talks = document.querySelectorAll("div.talk");

            var j = 0;
            for (var i = 0; i < messages.length; i++) {
                var m = messages[i];

                var id = m.Id;
                var avatarUrl = m.Avatar;
                var displayName = m.DisplayName;
                var publishedAt = m.PublishedAt;
                var content = m.Content;
                var isMerged = m.IsMerged;

                for (; ;) {
                    var prev = talks[j];
                    var aft = talks[j + 1];
                    var pid = prev ? parseInt(prev.getAttribute("data-message-id"), 10) : -1;
                    var aid = aft ? parseInt(aft.getAttribute("data-message-id"), 10) : Number.MAX_VALUE;

                    if (id == pid) {
                        var talk = createTaklElement(m);
                        document.body.insertBefore(talk, prev);
                        prev.remove();
                        break;
                    } if (pid < id && id < aid) {
                        var talk = createTaklElement(m);
                        if (aft) {
                            document.body.insertBefore(talk, aft);
                        } else {
                            document.body.appendChild(talk);
                        }
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
        } catch (ex) {
            talk.innerText = ex;
        }

        return talk;
    }
})();