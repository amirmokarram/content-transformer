(function ($) {
    "use strict";
    var container = $("div[transformer-container]");
    fetch("transformer-api/transforms", {
            headers: {
                "Content-Type": "application/json; charset=utf-8"
            }
        })
        .then(response => response.json())
        .then(transformers => {
            for (let i = 0; i < transformers.length; i++) {
                var transformerEl = $("<div>");

                var nameRowEl = $("<div>");
                var nameTitleEl = $("<div title>Name:</div>");
                var nameEl = $("<div value>");
                nameEl.text(transformers[i].Name);
                nameRowEl.append(nameTitleEl);
                nameRowEl.append(nameEl);
                transformerEl.append(nameRowEl);

                var sourceRowEl = $("<div>");
                var sourceTitleEl = $("<div title>Source:</div>");
                var sourceEl = $("<div value>");
                sourceEl.text(transformers[i].SourceIdentity);
                sourceRowEl.append(sourceTitleEl);
                sourceRowEl.append(sourceEl);
                transformerEl.append(sourceRowEl);

                var actionRowEl = $("<div action>");
                var buttonEl = $("<a class='btn btn-primary' href='transformer-api/transform/" + transformers[i].Id + "' role='button'>Export</a>");
                actionRowEl.append(buttonEl);
                transformerEl.append(actionRowEl);

                container.append(transformerEl);
            }
        }).catch(error => {
            alert(error);
        });
}(window.jQuery));