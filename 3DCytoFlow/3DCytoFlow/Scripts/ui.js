//// //// //// //// initalizations //// //// //// //// 
// info
$("#info-box").toggleClass("hidden");
//// //// //// //// file upload //// //// //// ////  

var userFile;
var data;

// file selector so we can hide ugly input
var fileSelector = $("#fileSelector");
var fileInput = $("#fileInput");


// These next two functions are unnecessary... however
// I feel that even the slight glimpse of the loader
// gives a polished look to the UI

function hideAndLoad() {
    $(".ui.modal").modal("hide");
    $(".ui.sidebar").sidebar("hide");
    $("#loader").addClass("ui active dimmer");
}
function plot(name) {

    setTimeout(function () {

        fetchData(name);

        if (!plotData()) {
           alert("This file is corrupted or does not exist");
            return;
        }

        $("#loader").removeClass("ui active dimmer");

        updateInfoBox();
         
    }, 200);

    hideAndLoad();
}

// popups

$("#side-explanation a").popup({ position: "right center" });


//// //// //// //// info box //// //// //// //// 
// aka glowing / pulsing effect
$(".info.icon")
  .transition("set looping")
  .transition("pulse", "1500ms");

$(".info.icon").click(function () {
    $("#info-box").toggleClass("hidden");
});

function updateInfoBox() {
    // total size
    $("#statsSize").empty();
    $("#statsSize").append(_.size(organized));

    // number clusters
    $("#clusterSize").empty();
    $("#clusterSize").append(_.uniq(organized, Object.keys(data)[3]).length);
    $("#info-box").removeClass("hidden");

    // cluster info
    $("#clusterInfo").empty();
    for (var i = 0; i < groupedSize; i++) {
        $("#clusterInfo").append(
          "<div style=\"color:" + colors[i] + ";\">" + groupedKeys[i] + " : " + grouped[groupedKeys[i]].length + "</div>"
          );
    }
}

//initalize rotateToggle
$(".ui.checkbox")
  .checkbox();

$(".ui.checkbox").click(function () {
    rotateToggle();
});

$(".ui.undo").click(function () {
    resetCamera();
});



