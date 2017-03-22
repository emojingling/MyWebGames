var colormethod = function() {
    return {
        RGBToHex: function(rgb) {
            var regexp = /[0-9]{0,3}/g;
            var re = rgb.match(regexp); //利用正则表达式去掉多余的部分，将rgb中的数字提取
            var hexColor = "#";
            var hex = ['0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'];
            for (var i = 0; i < re.length; i++) {
                var r = null, c = re[i], l = c;
                var hexAr = [];
                while (c > 16) {
                    r = c % 16;
                    c = (c / 16) >> 0;
                    hexAr.push(hex[r]);
                }
                hexAr.push(hex[c]);
                if (l < 16 && l !== "") {
                    hexAr.push(0);
                }
                hexColor += hexAr.reverse().join('');
            }
            return hexColor;
        }
    }
}();