#define KUZU_EXPORTS
#define _WIN32

%module kuzunet
%{
/* Put header files here or function declarations like below */
#include "kuzu.h"
%}

%include "typemaps.i"
%include "arrays_csharp.i"

// Map fixed-width integer types to C# equivalents
%apply unsigned char { uint8_t };
%apply signed char { int8_t };

%apply unsigned short { uint16_t };
%apply short { int16_t };

%apply unsigned int { uint32_t };
%apply int { int32_t };

%apply unsigned long long { uint64_t };
%apply long long { int64_t };

// Mark functions that allocate a fresh kuzu_value so SWIG sets swigCMemOwn=true
%newobject kuzu_value_create_null;
%newobject kuzu_value_create_null_with_data_type;
%newobject kuzu_value_create_default;
%newobject kuzu_value_create_bool;
%newobject kuzu_value_create_int8;
%newobject kuzu_value_create_int16;
%newobject kuzu_value_create_int32;
%newobject kuzu_value_create_int64;
%newobject kuzu_value_create_uint8;
%newobject kuzu_value_create_uint16;
%newobject kuzu_value_create_uint32;
%newobject kuzu_value_create_uint64;
%newobject kuzu_value_create_int128;
%newobject kuzu_value_create_float;
%newobject kuzu_value_create_double;
%newobject kuzu_value_create_internal_id;
%newobject kuzu_value_create_date;
%newobject kuzu_value_create_timestamp_ns;
%newobject kuzu_value_create_timestamp_ms;
%newobject kuzu_value_create_timestamp_sec;
%newobject kuzu_value_create_timestamp_tz;
%newobject kuzu_value_create_timestamp;
%newobject kuzu_value_create_interval;
%newobject kuzu_value_create_string;
%newobject kuzu_value_clone;


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

// Correct borrowed kuzu_value** patterns -> out kuzu_value (non-owning wrapper)
// Remove previous incorrect direct mapping.
%typemap(cstype) (kuzu_value **out_value) "out kuzu_value";
%typemap(imtype) (kuzu_value **out_value) "out kuzu_value";
%typemap(csin) (kuzu_value **out_value) "out $csinput";

%typemap(cstype) (kuzu_value **out_key) "out kuzu_value";
%typemap(imtype) (kuzu_value **out_key) "out kuzu_value";
%typemap(csin) (kuzu_value **out_key) "out $csinput";

// If needed additional out pointers can be added similarly.

%typemap(cstype) char **out_column_name "out string";
%typemap(imtype) char **out_column_name "out string";
%typemap(csin) char **out_column_name "out $csinput";

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

// Ignore the Arrow array stuff -- I'm not sure how to get SWIG to handle it
%ignore ArrowArray;
%ignore ArrowSchema;
%ignore ARROW_FLAG_DICTIONARY_ORDERED;
%ignore ARROW_FLAG_MAP_KEYS_SORTED;
%ignore ARROW_FLAG_NULLABLE;
%ignore kuzu_query_result_get_arrow_schema;
%ignore kuzu_query_result_get_next_arrow_chunk;

// Initial attempt at ArrowArray stuff
// %apply void *VOID_INT_PTR { void ** };
// %apply void *VOID_INT_PTR { void * };
// %typemap(cstype) ArrowArray **children "ArrowArray[]"


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
// Stop ignoring _is_owned_by_cpp so we could (optionally) inspect ownership.
//%ignore _is_owned_by_cpp;

// Replace destructor and dispose to call destroy only when owning.
// This avoids double free of borrowed objects.
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
        if (swigCMemOwn) {
          swigCMemOwn = false;
          $modulePINVOKE.$csclassname_destroy($csclassname.getCPtr(this));
        }
        swigCPtr = new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero);
      }
    }
  }
%}


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