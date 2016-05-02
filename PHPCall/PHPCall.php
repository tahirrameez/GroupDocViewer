<?php 

  $client = new SoapClient("http://localhost/ItemWS/ERP_Sync.asmx?wsdl",

    array(

      "trace"      => 1,                            // enable trace to view what is happening

      "exceptions" => 0,                     // disable exceptions

      "cache_wsdl" => 0)                   // disable any caching on the wsdl, encase you alter the wsdl server

  );

 

  // get a response from the WSDL zend server function getQuote for the day monday

  print_r( $client->getColorData(array("userName" => "Admin", "password" =>"admin@123")));

 

  // display what was sent to the server (the request)

  echo "<p>Request :".htmlspecialchars($client->__getLastRequest()) ."</p>";

  // display the response from the server

  echo "<p>Response:".htmlspecialchars($client->__getLastResponse())."</p>";

?>
