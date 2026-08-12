var inputConfig = {

    field_metadata: {

        column_name: {
            maxlength: 100,
            type: "text",
            placeholder: "Column Name"
        },
        display_label: {
            maxlength: 500,
            type: "text",
            placeholder: "Label / Heading"
        },
        placeholder: {
            maxlength: 100,
            type: "text",
            placeholder: "Placeholder"
        },
        tooltip: {
            maxlength: 500,
            type: "text",
            placeholder: "Tooltip Text"
        },
        section_name: {
            maxlength: 100,
            type: "text",
            placeholder: "Section Name"
        },
        section_number: {
            type: "number",
            placeholder: "Section Number"
        },
        sql_type: {
            type: "select",
            options: [
                "int", "int identity", "smallint", "bigint", "bit", "decimal", "float", "double", "date", "datetime", "char", "varchar", "nchar", "nvarchar", "varchar(max)", "nvarchar(max)", "text", "ntext", "uniqueidentifier", "varbinary", "image", "xml"
            ]
        },
        input_type: {
            type: "select",
            options: [
                "number", "text", "select", "date", "datetime-local", "time", "checkbox", "radio", "textarea", "password", "file", "hidden", "email", "url", "tel", "color", "range", "month", "week", "search", "submit", "reset", "button"
            ]
        },


        is_required: {
            type: "select",
            options: [
                { value: false, text: "Optional" },
                { value: true, text: "Mandatory" }
            ]
        },
        is_readonly: {
            type: "select",
            options: [
                { value: false, text: "No" },
                { value: true, text: "Yes" }
            ]
        },
        is_computed: {
            type: "select",
            options: [
                { value: false, text: "No" },
                { value: true, text: "Yes" }
            ]
        },
        is_active: {
            type: "select",
            options: [
                { value: false, text: "No" },
                { value: true, text: "Yes" }
            ]
        },
        allow_insert: {
            type: "select",
            options: [
                { value: true, text: "Yes" },
                { value: false, text: "No" },
            ]
        },
        allow_update: {
            type: "select",
            options: [
                { value: true, text: "Yes" },
                { value: false, text: "No" }
            ]
        },
        show_in_list: {
            type: "select",
            options: [
                { value: true, text: "Yes" },
                { value: false, text: "No" },
            ]
        },
        is_searchable: {
            type: "select",
            options: [
                { value: true, text: "Yes" },
                { value: false, text: "No" },
            ]
        },
        is_sortable: {
            type: "select",
            options: [
                { value: true, text: "Yes" },
                { value: false, text: "No" },
            ]
        },
        exportable: {
            type: "select",
            options: [
                { value: true, text: "Yes" },
                { value: false, text: "No" },
            ]
        },
        importable: {
            type: "select",
            options: [
                { value: true, text: "Yes" },
                { value: false, text: "No" },
            ]
        },
        is_deleted: {
            type: "select",
            options: [
                { value: false, text: "No" },
                { value: true, text: "Yes" },
            ]
        },
        is_multi_select: {
            type: "select",
            options: [
                { value: false, text: "No" },
                { value: true, text: "Yes" },
            ]
        },
        is_foreign_key: {
            type: "select",
            options: [
                { value: false, text: "No" },
                { value: true, text: "Yes" },
            ]
        },
        dropdown_source_table: {
            type: "select",
            options: []
        },
        dropdown_value_column: {
            type: "select"
        },
        dropdown_text_column: {
            type: "select"
        },
        dropdown_where: {
            type: "text"
        },
        dropdown_order_by: {
            type: "text"
        },
        is_foreign_key: {
            type: "select",
            options: [
                { value: false, text: "No" },
                { value: true, text: "Yes" },
            ]
        },
        decimal_places: {
            type: "number"
        },
        sort_order: {
            type: "number"
        },
        min_length: {
            type: "number"
        },
        max_length: {
            type: "number"
        },
        min_value: {
            type: "number"
        },
        max_value: {
            type: "number"
        }

        // Add more tables...
    }
}