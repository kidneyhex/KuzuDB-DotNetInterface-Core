#define KUZU_EXPORTS
#define _WIN32

%module kuzunet
%{
/* Put header files here or function declarations like below */
#include "kuzu.h"
%}

%include "typemaps.i"


%apply unsigned char { uint8_t };
%apply signed char { int8_t };
%apply unsigned short { uint16_t };
%apply short { int16_t };
%apply unsigned int { uint32_t };
%apply int { int32_t };
%apply unsigned long long { uint64_t };
%apply long long { int64_t };


// kuzu_connection
%typemap(cstype) (kuzu_connection *out_connection) "out $csclassname";
%typemap(csin, pre="    $1_name = new $csclassname();") 
	(kuzu_connection *out_connection) "$csclassname.getCPtr($csinput)";

// kuzu_database
%typemap(cstype) (kuzu_database *out_database) "out $csclassname";
%typemap(csin, pre="    $1_name = new $csclassname();") 
	(kuzu_database *out_database) "$csclassname.getCPtr($csinput)";

// kuzu_query_result
%typemap(cstype) (kuzu_query_result *out_query_result) "out $csclassname";
%typemap(csin, pre="    $1_name = new $csclassname();") 
	(kuzu_query_result *out_query_result) "$csclassname.getCPtr($csinput)";

// ArrowSchema
%typemap(cstype) (ArrowSchema *out_schema) "out $csclassname";
%typemap(csin, pre="    $1_name = new $csclassname();") 
	(ArrowSchema *out_schema) "$csclassname.getCPtr($csinput)";

// kuzu_value
%typemap(cstype) (kuzu_value *out_value) "out $csclassname";
%typemap(csin, pre="    $1_name = new $csclassname();") 
	(kuzu_value *out_value) "$csclassname.getCPtr($csinput)";

// kuzu_flat_tuple
%typemap(cstype) (kuzu_flat_tuple *out_flat_tuple) "out $csclassname";
%typemap(csin, pre="    $1_name = new $csclassname();") 
	(kuzu_flat_tuple *out_flat_tuple) "$csclassname.getCPtr($csinput)";


// Pass strings around letting C# handle marshalling
%typemap(cstype) (char **out_result) "out string"
%typemap(imtype) (char **out_result) "out string"
%typemap(csin) (char **out_result) "out $csinput"


// Map the kuzu_value_get_{type} methods to use "out {type}"
%apply signed char *OUTPUT { int8_t *out_result };
%apply unsigned char *OUTPUT { uint8_t *out_result };

%apply short *OUTPUT { int16_t *out_result };
%apply unsigned short *OUTPUT { uint16_t *out_result };

%apply int *OUTPUT { int32_t *out_result};
%apply unsigned int *OUTPUT { uint32_t *out_result};

%apply long *OUTPUT { int64_t *out_result};
%apply unsigned long *OUTPUT { uint64_t *out_result};

%apply bool *OUTPUT { bool *out_result };
%apply float *OUTPUT { float *out_result };
%apply double *OUTPUT { double *out_result };


//%apply long long *OUTPUT { int128_t *out_result};
//%apply unsigned long long *OUTPUT { uint128_t *out_result};


// Replace destructor and dispose to call destroy
%typemap(csdispose) 
kuzu_connection, 
kuzu_database, 
kuzu_value, 
kuzu_prepared_statement, 
kuzu_flat_tuple, 
kuzu_data_type, 
kuzu_query_summary,
kuzu_query_result
%{
	public void Destroy() {
		$modulePINVOKE.$1_type_destroy($csclassname.getCPtr(this));
	}

  ~$csclassname() {
    Dispose();
  }

  public void Dispose() {
    $modulePINVOKE.$1_type_destroy($csclassname.getCPtr(this));
    Dispose(true);
    global::System.GC.SuppressFinalize(this);
  }
%}

// // Add {class}.Destroy for the following:
// %typemap(cscode) 
// kuzu_connection, 
// kuzu_database, 
// kuzu_value, 
// kuzu_prepared_statement, 
// kuzu_flat_tuple, 
// kuzu_data_type, 
// kuzu_query_summary,
// kuzu_query_result
// {%

// %}

// %typemap(cscode) kuzu_value 
// %{
// 	public string AsString() {
// 		$modulePINVOKE.kuzu_value_as_string($csclassname.getCPtr(this), out string result);
// 		return result;
// 	}

// 	public int AsInt32() {
// 		$modulePINVOKE.kuzu_value_as_int32($csclassname.getCPtr(this), out int result);
// 		return result;
// 	}

// 	public long AsInt64() {
// 		$modulePINVOKE.kuzu_value_as_int64($csclassname.getCPtr(this), out long result);
// 		return result;
// 	}	

// 	public short AsInt16() {
// 		$modulePINVOKE.kuzu_value_as_int16($csclassname.getCPtr(this), out short result);
// 		return result;
// 	}

// 	public double AsDouble() {
// 		$modulePINVOKE.kuzu_value_as_double($csclassname.getCPtr(this), out double result);
// 		return result;
// 	}

// 	public $csclassname Copy() {
// 		$modulePINVOKE.kuzu_value_copy
// 	}
// %}

// %delobject kuzu_connection_destroy;
// %delobject kuzu_database_destroy;
// %delobject kuzu_value_destroy;
// %delobject kuzu_query_result_destroy;
// %delobject kuzu_data_type_destroy;
// %delobject kuzu_flat_tuple_destroy;
// %delobject kuzu_query_summary_destroy;
// %delobject kuzu_prepared_statement_destroy;



%include <windows.i>
%include "kuzu.h"

