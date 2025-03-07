#define KUZU_EXPORTS
#define _WIN32

%module kuzunet
%{
/* Put header files here or function declarations like below */
#include "kuzu.h"
%}




%include "typemaps.i"
%include "arrays_csharp.i"

//%pragma(csharp) moduleclassmodifiers="internal sealed class";
//%typemap(csclassmodifiers) SWIGTYPE "internal sealed class";

%apply unsigned char { uint8_t };
%apply signed char { int8_t };

%apply unsigned short { uint16_t };
%apply short { int16_t };

%apply unsigned int { uint32_t };
%apply int { int32_t };

%apply unsigned long long { uint64_t };
%apply long long { int64_t };


// -----------------
// If you want to have things like kuzu_connection_init create a 
//   connection and pass it as an "out" parameter you could do this
//   Side effect is that disposing with using statements gets harder

// // kuzu_connection
// %typemap(cstype) (kuzu_connection *out_connection) "$csclassname";
// %typemap(csin, pre="    $1_name = new $csclassname();") 
// 	(kuzu_connection *out_connection) "$csclassname.getCPtr($csinput)";

// // kuzu_database
// %typemap(cstype) (kuzu_database *out_database) "$csclassname";
// %typemap(csin, pre="    $1_name = new $csclassname();") 
// 	(kuzu_database *out_database) "$csclassname.getCPtr($csinput)";

// // kuzu_query_result
// %typemap(cstype) (kuzu_query_result *out_query_result) "$csclassname";
// %typemap(csin, pre="    $1_name = new $csclassname();") 
// 	(kuzu_query_result *out_query_result) "$csclassname.getCPtr($csinput)";

// // ArrowSchema
// %typemap(cstype) (ArrowSchema *out_schema) "$csclassname";
// %typemap(csin, pre="    $1_name = new $csclassname();") 
// 	(ArrowSchema *out_schema) "$csclassname.getCPtr($csinput)";

// // kuzu_value
// %typemap(cstype) (kuzu_value *out_value) "$csclassname";
// %typemap(csin, pre="    $1_name = new $csclassname();") 
// 	(kuzu_value *out_value) "$csclassname.getCPtr($csinput)";

// // kuzu_flat_tuple
// %typemap(cstype) (kuzu_flat_tuple *out_flat_tuple) "$csclassname";
// %typemap(csin, pre="    $1_name = new $csclassname();") 
// 	(kuzu_flat_tuple *out_flat_tuple) "$csclassname.getCPtr($csinput)";


// --------------
// Pass strings around letting C# handle marshalling
%typemap(cstype) (char **out_result) "out string";
%typemap(imtype) (char **out_result) "out string";
%typemap(csin) (char **out_result) "out $csinput";

// Ignore blob and strings; C# will handle destroying (maybe?)
%ignore kuzu_destroy_string;
%ignore kuzu_destroy_blob;
%typemap(cstype) (uint8_t **out_result) "out byte[]";
%typemap(imtype) (uint8_t **out_result) "out byte[]";
%typemap(csin) (uint8_t **out_result) "out $csinput";

%typemap(cstype) (kuzu_value **out_value) "kuzu_value";
%typemap(imtype) (kuzu_value **out_value) "kuzu_value";
%typemap(csin) (kuzu_value **out_value) "$csinput";

%typemap(cstype) char **out_column_name "out string";
%typemap(imtype) char **out_column_name "out string";
%typemap(csin) char **out_column_name "out $csinput";

%typemap(cstype) (char **field_names) "string[]";
%typemap(imtype) (char **field_names) "string[]";
%typemap(csin) (char **field_names) "$csinput";

%typemap(cstype) 
SWIGTYPE **elements, 
SWIGTYPE **values, 
SWIGTYPE **field_values,
SWIGTYPE **keys
"$1_basetype[]";

%typemap(imtype) 
SWIGTYPE **elements, 
SWIGTYPE **values, 
SWIGTYPE **field_values,
SWIGTYPE **keys
"$1_basetype[]"

%typemap(csin) 
SWIGTYPE **elements, 
SWIGTYPE **values, 
SWIGTYPE **field_values,
SWIGTYPE **keys
"$csinput"

// Ignore the Arrow array stuff
%ignore ArrowArray;
%ignore ArrowSchema;
%ignore ARROW_FLAG_DICTIONARY_ORDERED;
%ignore ARROW_FLAG_MAP_KEYS_SORTED;
%ignore ARROW_FLAG_NULLABLE;
%ignore kuzu_query_result_get_arrow_schema;
%ignore kuzu_query_result_get_next_arrow_chunk;

// --------------------
// Map the kuzu_value_get_{type} methods to use "out {type}"
%apply signed char *OUTPUT { int8_t *out_result };
%apply unsigned char *OUTPUT { uint8_t *out_result };

%apply short *OUTPUT { int16_t *out_result };
%apply unsigned short *OUTPUT { uint16_t *out_result };

%apply int *OUTPUT { int32_t *out_result};
%apply unsigned int *OUTPUT { uint32_t *out_result};

%apply long long *OUTPUT { int64_t *out_result};
%apply unsigned long long *OUTPUT { uint64_t *out_result};
%apply unsigned long long *OUTPUT { uint64_t *out_value};

%apply bool *OUTPUT { bool *out_result };
%apply float *OUTPUT { float *out_result };
%apply double *OUTPUT { double *out_result };


// ------ 
// Ignore private members of structs
%ignore _connection;
%ignore _database;
%ignore _flat_tuple;
%ignore _data_type;
%ignore _value;
%ignore _prepared_statement;
%ignore _query_result;
%ignore _query_summary;
%ignore _bound_values;
%ignore _is_owned_by_cpp;

// // ------------------
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
// %{
// 	public void Destroy() {
//         $modulePINVOKE.$1_type_destroy($csclassname.getCPtr(this));
// 	}
// %}


// Replace destructor and dispose to call destroy automatically
// This way both the SWIG wrapper and kuzu object get disposed by using statements
%typemap(csdisposing, methodname="Dispose", methodmodifiers="protected", parameters="bool disposing") 
kuzu_connection, 
kuzu_database, 
kuzu_value, 
kuzu_prepared_statement, 
kuzu_flat_tuple, 
kuzu_data_type, 
kuzu_query_summary,
kuzu_query_result
%{
  {
    lock(this) {
      if (swigCPtr.Handle != global::System.IntPtr.Zero) {
        $modulePINVOKE.$csclassname_destroy($csclassname.getCPtr(this));

        if (swigCMemOwn) {
          swigCMemOwn = false;
          $modulePINVOKE.delete_$csclassname(swigCPtr);
        }

        swigCPtr = new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero);
      }
    }
  }
%}


// --------------------------
// Experimenting with adding methods to kuzu_value
// Note: should add these as a partial class or extension methods?

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

// %}


%include <windows.i>
%include "kuzu.h"


// Not sure how to make SWIG give tm a class; so doing it manually...
%inline %{
#ifdef SWIG
typedef struct {
    int tm_sec;   // seconds after the minute - [0, 60] including leap second
    int tm_min;   // minutes after the hour - [0, 59]
    int tm_hour;  // hours since midnight - [0, 23]
    int tm_mday;  // day of the month - [1, 31]
    int tm_mon;   // months since January - [0, 11]
    int tm_year;  // years since 1900
    int tm_wday;  // days since Sunday - [0, 6]
    int tm_yday;  // days since January 1 - [0, 365]
    int tm_isdst; // daylight savings time flag
} tm;
#endif
%}