/// <reference path="jquery-1.8.2.js" />
/// <reference path="_references.js" />
/// <reference path="~/Scripts/jquery-2.1.4.intellisense.js" />
var maxRetries = 3;
var blockLength = 1048576;
var numberOfBlocks = 1;
var currentChunk = 1;
var retryAfterSeconds = 3;

$(document).ready(function ()
{
    $(document).on("click", "#fileUpload", beginUpload);

    $("#validator").bootstrapValidator({
        live: "enabled",
        feedbackIcons: {
            invalid: "glyphicon glyphicon-remove",
            validating: "glyphicon glyphicon-refresh"
        },
        fields: {
            fileupload: {
                validators: {
                   file: {
                       extension: "fcs",
                       type: "text/plain",
                       message: "The selected file is not valid. The file extension should be .fcs"
                   }
                }
            },
            patient: {
                validators: {
                    notEmpty: {
                        message: "The patient is required and can't be empty"
                    }
                }
            }
        }
        })
        .on("success.form.bv", function(e) {
            e.preventDefault();
            $("#validator").data("bootstrapValidator").disableSubmitButtons(true);
        });
    });

var beginUpload = function () {
    $("#fileUpload").prop("disabled", true);
    $("#progressBar").css("width", parseInt(0) + "%");
    var fileControl = document.getElementById("selectFile");
    if (fileControl.files.length > 0) {
        for (var i = 0; i < fileControl.files.length; i++) {
            uploadMetaData(fileControl.files[i]);
        }
    }
}

var uploadMetaData = function (file)
{
    var size = file.size;
    numberOfBlocks = Math.ceil(file.size / blockLength);
    var name = file.name;
    currentChunk = 1;
    var patient = $("#patients option:selected").text();

    $.ajax({
        type: "POST",
        async: false,
        url: "/Account/SetMetadata?blocksCount=" + numberOfBlocks + "&fileName=" + name + "&fileSize=" + size + "&patient=" + patient
    }).done(function (state)
    {
        if (state === true)
        {
            displayStatusMessage("Starting Upload");
            sendFile(file, blockLength);
        }
    }).fail(function ()
    {
        displayStatusMessage("Failed to send MetaData");
    });

}

var sendFile = function (file, chunkSize)
{
    var start = 0,
        end = Math.min(chunkSize, file.size),
        retryCount = 0, fileChunk;
    displayStatusMessage("");

    var sendNextChunk = function ()
    {
        fileChunk = new FormData();

        if (file.slice)
        {
            fileChunk.append("Slice", file.slice(start, end));
        }
        else if (file.webkitSlice)
        {
            fileChunk.append("Slice", file.webkitSlice(start, end));
        }
        else if (file.mozSlice)
        {
            fileChunk.append("Slice", file.mozSlice(start, end));
        }
        else
        {
            displayStatusMessage(window.operationType.UNSUPPORTED_BROWSER);
            return;
        }
        var jqxhr = $.ajax({
            async: true,
            url: ("/Account/UploadChunk?id=" + currentChunk),
            data: fileChunk,
            cache: false,
            contentType: false,
            processData: false,
            type: "POST"
        }).fail(function (request, error)
        {
            if (error !== "abort" && retryCount < maxRetries)
            {
                ++retryCount;
                setTimeout(sendNextChunk, retryAfterSeconds * 1000);
            }

            if (error === "abort")
            {
                displayStatusMessage("Aborted");
            }
            else
            {
                if (retryCount === maxRetries)
                {
                    displayStatusMessage("Upload timed out.");
                    window.resetControls();
                    var uploader = null;
                }
                else
                {
                    displayStatusMessage("Resuming Upload");
                }
            }

            return;
        }).done(function (notice)
        {       
            if (notice.error || notice.isLastBlock)
            {
                if (notice.isLastBlock)
                {
                    $("#progressBar").css("width", "100%");
                    $("#fileUpload").prop("disabled", false);
                    $("#message").css("display", "inline-block");
                }
                displayStatusMessage(notice.message);
                return;
            }
            ++currentChunk;
            start = (currentChunk - 1) * blockLength;
            end = Math.min(currentChunk * blockLength, file.size);
            retryCount = 0;
            updateProgress();
            if (currentChunk <= numberOfBlocks)
            {
                sendNextChunk();
            }
        });
    };
    sendNextChunk();
}

var displayStatusMessage = function (message)
{
    $("#statusMessage").text(message);
}

var updateProgress = function ()
{
    var progress = currentChunk / numberOfBlocks * 100;
    if (progress <= 100)
    {
        $("#progressBar").css("width", parseInt(progress) + "%");
        displayStatusMessage("Uploaded " + progress + "%");
    }
}