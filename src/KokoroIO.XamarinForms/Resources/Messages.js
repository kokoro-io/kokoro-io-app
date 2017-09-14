(function () {

    function asArray(messages) {
        if (!messages) {
            return null;
        } else if (Array.isArray(messages)) {
            return messages;
        }
        return JSON.parse(messages);
    }

    var setMessages = window.setMessages = function (messages) {
        document.body.innerHTML = "";

        messages = asArray(messages);

        if (!messages) {
            return;
        }

        for (var i = 0; i < messages.length; i++) {
            var m = messages[i];

            var id = m.Id;
            var avatarUrl = m.Avatar;
            var displayName = m.DisplayName;
            var publishedAt = m.PublishedAt;
            var content = m.Content;
            var isMerged = m.IsMerged;

            var talk = createTaklElement(m);
            document.body.appendChild(talk);
        }
    }
    var addMessages = window.addMessages = function (messages) {
        var talks = document.querySelectorAll("div.talk");

        messages = asArray(messages);

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

                if (pid < id && id < aid) {
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

    function createTaklElement(m) {
        var id = m.Id;
        var avatarUrl = m.Avatar;
        var displayName = m.DisplayName;
        var publishedAt = m.PublishedAt;
        var content = m.Content;
        var isMerged = m.IsMerged;

        var talk = document.createElement("div");
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
    var setMessage = window.setMessage = function (id, avatar, displayName, publishedAt, content, isMerged) {
        setMessages([{
            Id: parseInt(Id, 10),
            Avatar: avatar,
            DisplayName: displayName,
            PublishedAt: publishedAt,
            Content: content,
            IsMerged: !!isMerged
        }]);
    }
})();
