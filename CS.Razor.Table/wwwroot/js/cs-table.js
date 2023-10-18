        window.applyHeaderStyle = function (id, classes) {
            document.getElementById(id).className = 'bi ';
            document.getElementById(id).classList.add(classes);
        }


        window.removeHeaderStyle = function (id) {
            document.getElementById(id).classList = '';
        }