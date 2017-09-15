(function () {
    var setMessages = window.setMessages = function (messages) {
        document.body.innerHTML = "";
        addMessages(messages, null);
    }

    var addMessages = window.addMessages = function (messages, merged) {
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