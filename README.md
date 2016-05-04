
# GroupDocViewer Web API

This project contains WCF web service  written in C# to expose Group Doc Viewer functionality. This web API will convert files sent to it into HTML or Jpeg. Web Service has two end points, SOAP and REST. Project also contains PHP code that consumes the WCF Web Service.

## Setup

You will need to Clone this repo at your local machine. and install group doc viewer from following link, 
http://groupdocs.com/Community/files/8/.net-libraries/groupdocs_viewer_for_.net/category1002.aspx

If license is not purchased for group doc viewer it will only convert first two pages of file.

### Prerequisites

Visual Studio 2012 or greater.
Group Doc Viewer 3.x
PHP 5.5


### Installing & Deployment

After installing the Group doc Viewer 3.x 
There are two ways to install and test the web service,
1-Inside the visual Studio by using IIS Express
2-Publishing the web service and deploying it on IIS

For First Scenario 
After opening the WCF code in Visual studio, you will need to select the gdweb.SVC and run the project, after successful completion  of build, Service Path will be shown, that path will have to be used in PHP code for consuming the web service.
 
 <script type='text/javascript'> $.ui.groupdocsViewer.prototype.applicationPath = 'http://localhost:10686/GDWeb.svc';</script>

For Second Scenario

After opening the project by clicking on GDWebAPI.sln file. Publish the project and deploy it in IIS, and update the path according to the system name, e.g. if the system name is "LiveSystem" and port is 1028 link should be updated as follows,

 <script type='text/javascript'> $.ui.groupdocsViewer.prototype.applicationPath = 'http://LiveSystem:1028/GDWeb.svc';</script>


## Built With

* Visual Studio 2012
* Dream Weaver



## Versioning

This is Version  1.0 of project.

## Authors

* **Tahir Rameez** - *Initial work* - (https://github.com/rahirrameez)



## License

This project is licensed under the MIT License 


